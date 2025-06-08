using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Clase central que gestiona el flujo del juego. 
/// Controla el progreso del jugador, la gestión de escenas, los menús de UI, 
/// el sistema de respawn y la interacción con Firebase y PlayerPrefs.
/// </summary>

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public Ads adsManager;


    [Header("Prefabs de UI")]
    public GameObject pauseMenuPrefab;
    public GameObject menuMuertePrefab;

    private GameObject pauseMenuInstance;
    private GameObject menuMuerteInstance;

    private Vector3 ultimaPosicion;
    private GameObject ultimaTrampa;
    private GameObject caja;

    private bool primerRespawnHecho = false;

    /// Configura la instancia Singleton y registra OnSceneLoaded.

    private int ExtraerNumero(string sceneName)
    {
        if (sceneName.StartsWith("Scene") && int.TryParse(sceneName.Substring(5), out int num))
            return num;
        return 0;
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// Espera a que SaveSystem esté disponible y guarda el progreso.

    private IEnumerator EsperarYGuardarProgreso(string nivel)
    {
        float timeout = 3f; // Tiempo máximo de espera
        float timer = 0f;

        while (SaveSystem.Instance == null && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.GuardarProgreso(nivel);
            Debug.Log("Progreso enviado a SaveSystem: " + nivel);
        }
        else
        {
            Debug.LogWarning("No se pudo guardar en Firestore: SaveSystem no disponible tras esperar.");
        }
    }

    /// Configura menús de UI y guarda el progreso al cargar una escena.

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        if (sceneName.StartsWith("Scene"))
        {
            // Guardar progreso
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.GuardarProgreso(sceneName);
            }
            else
            {
                // Si por algún motivo SaveSystem no está disponible, como fallback puedes usar la lógica que ya tienes:
                string progresoActual = PlayerPrefs.GetString("ProgresoGuardado", "Scene101");

                int nuevo = ExtraerNumero(sceneName);
                int actual = ExtraerNumero(progresoActual);

                if (nuevo > actual)
                {
                    PlayerPrefs.SetString("ProgresoGuardado", sceneName);
                    PlayerPrefs.Save();
                    Debug.Log("Progreso local guardado (fallback): " + sceneName);
                }
            }


            // Instanciar menú de pausa si no existe
            if (pauseMenuInstance == null && pauseMenuPrefab != null)
            {
                pauseMenuInstance = Instantiate(pauseMenuPrefab);
                DontDestroyOnLoad(pauseMenuInstance);
            }

            // Destruir menú de muerte de la escena anterior (si existía) y preparar uno nuevo, desactivado
            if (menuMuerteInstance != null)
            {
                Destroy(menuMuerteInstance);
                menuMuerteInstance = null;
            }
            if (menuMuertePrefab != null)
            {
                menuMuerteInstance = Instantiate(menuMuertePrefab);
                menuMuerteInstance.SetActive(false);
                DontDestroyOnLoad(menuMuerteInstance);
            }

            // Encontrar la caja
            caja = GameObject.FindGameObjectWithTag("Caja");

            StartCoroutine(EsperarYGuardarProgreso(sceneName));

        }
        else
        {
            // Si es una escena no jugable (por ejemplo, menú principal), destruimos ambos menús
            if (pauseMenuInstance != null)
            {
                Destroy(pauseMenuInstance);
                pauseMenuInstance = null;
            }
            if (menuMuerteInstance != null)
            {
                Destroy(menuMuerteInstance);
                menuMuerteInstance = null;
            }
            if (sceneName == "MenuPrincipal")
            {
                primerRespawnHecho = false;
            }

        }
    }

    /// Carga una escena por nombre.

    public void CargarNivel(string nombre)
    {
        Debug.Log("Cargando escena: " + nombre);
        SceneManager.LoadScene(nombre);
    }

    /// Sale del juego.

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    /// Guarda la última posición de la caja.

    public void RegistrarPosicion(Vector3 posicion)
    {
        ultimaPosicion = posicion;
    }

    /// Muestra el menú de muerte y gestiona el respawn.

    public void Morir(GameObject trampa)
    {
        if (menuMuerteInstance == null)
        {
            Debug.LogWarning("No existe el prefab de menú de muerte o no se instanció correctamente.");
            return;
        }

        if (menuMuerteInstance.activeSelf)
        {
            return;
        }

        ultimaTrampa = trampa;
        trampa.GetComponent<Trampa>()?.DesactivarTrampa();

        menuMuerteInstance.SetActive(true);
        menuMuerteInstance.GetComponent<MenuMuerteUI>().Configurar(trampa.transform.position);

        Time.timeScale = 0f;
    }

    /// Llama al proceso de respawn.

    public void Respawn()
    {
        if (ultimaTrampa == null || caja == null)
        {
            Debug.LogWarning("No hay trampa ni caja configurada para respawnear.");
            menuMuerteInstance.SetActive(false);
            return;
        }
        if (!primerRespawnHecho)
        {
            // PRIMER RESPAWN Gratis
            Debug.Log("Primer respawn: sin anuncio.");
            Time.timeScale = 1f;
            StartCoroutine(RespawnCoroutine());
            primerRespawnHecho = true;
        }
        else
        {
            // SIGUIENTES RESPAWN Mostrar ad
            Debug.Log("Respawn con anuncio.");

            // Cargamos y mostramos el ad
            if (adsManager != null)
            {
                adsManager.LoadRewardedAd();
                StartCoroutine(EsperarYMostrarAd());

            }
            else
            {
                Debug.LogWarning("adsManager no asignado!");
                // Si por algún motivo no hay adsManager, al menos no rompemos el juego:
                Time.timeScale = 1f;
                StartCoroutine(RespawnCoroutine());
            }
        }



    }

    /// Realiza el proceso de respawn.

    private IEnumerator RespawnCoroutine()
    {
        // Desactivamos completamente la trampa (collider + sprite), por si acaso queda algo habilitado
        Collider2D col = ultimaTrampa.GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        SpriteRenderer sr = ultimaTrampa.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;

        yield return new WaitForSecondsRealtime(0.1f);

        // Movemos la caja a la posición original de la trampa
        caja.transform.position = ultimaTrampa.transform.position;

        // Reseteamos su velocidad (para evitar que siga “empujada”)
        Rigidbody2D rb = caja.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Reactivamos el script de movimiento de la caja
        CajaTest movimientoCaja = caja.GetComponent<CajaTest>();
        if (movimientoCaja != null)
            movimientoCaja.enabled = true;

        // Cerramos el menú de muerte
        if (menuMuerteInstance != null)
            menuMuerteInstance.SetActive(false);

        // Limpiamos la referencia para no respawnear otra vez inadvertidamente
        ultimaTrampa = null;
    }

    /// Reinicia la escena actual.

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// Carga el menú principal.

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    /// Realiza el respawn tras visualizar un anuncio.

    public void RespawnReal()
    {
        Debug.Log("Respawn después del anuncio.");

        Time.timeScale = 1f;
        StartCoroutine(RespawnCoroutine());
    }

    /// Espera la carga del anuncio y lo muestra.

    private IEnumerator EsperarYMostrarAd()
    {
        // Esperamos a que adLoaded sea true (máx 3 seg)
        float timeout = 3f;
        float timer = 0f;

        while (!adsManager.adLoaded && timer < timeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (adsManager.adLoaded)
        {
            adsManager.ShowRewardedAd();
        }
        else
        {
            Debug.LogWarning("No se pudo mostrar el ad: no cargado.");
        }
    }


}

