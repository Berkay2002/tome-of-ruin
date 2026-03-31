using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public string targetScene;
    public string entryPointId;

    private bool _transitioning;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_transitioning) return;
        if (!other.CompareTag("Player")) return;

        LoadScene(targetScene);
    }

    public void LoadScene(string sceneName)
    {
        if (_transitioning) return;
        _transitioning = true;
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        if (FadeScreen.Instance != null)
            yield return FadeScreen.Instance.FadeOut();

        SceneManager.LoadScene(sceneName);

        if (FadeScreen.Instance != null)
            yield return FadeScreen.Instance.FadeIn();

        _transitioning = false;
    }
}
