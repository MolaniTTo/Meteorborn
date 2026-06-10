using UnityEngine;

public class MenuMusicStarter : MonoBehaviour
{
    [SerializeField] private float fadeInTime = 1.5f;

    void Start()
    {
        //Nomes es reproduira la musica de menu si no hi ha cap musica reproduint-se ja
        if (AudioManager.Instance != null)
        {
            string currentMusic = AudioManager.Instance.GetCurrentMusic();

            //si no hi ha cap musica o la musica actual no es la de menu, llavors reproduim la de menu
            if (string.IsNullOrEmpty(currentMusic) || currentMusic != "Menu")
            {
                AudioManager.Instance.PlayMusic("Menu", fadeInTime);
            }
        }
    }
}