using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
        // Asegura que solo hay un GameManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persiste entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CargarEscena(string nombre)
    {
        SceneManager.LoadScene(nombre);
    }

    public void NivelCompletado()
    {
        CargarEscena("NivelCompletado");
    }

    public void VolverAlMenu()
    {
        CargarEscena("MenuPrincipal");
    }

    public void CargarNivel(string nombre)
    {
        CargarEscena(nombre);
    }

        public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}

