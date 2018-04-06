using UnityEngine;

public class LightConditions : MonoBehaviour
{
  private Light _light;
  private bool _transitionToNight = true;

  // Use this for initialization
  void Start()
  {
    _light = GetComponentInParent<Light>();
    InvokeRepeating("ChangeLighting", 2, 2);
  }

  void ChangeLighting()
  {
    float newIntensity = _transitionToNight ? _light.intensity - .01f : _light.intensity + .01f;
    _light.intensity = Mathf.Clamp(newIntensity, 0f, 1f);
    if (_light.intensity == 0 || _light.intensity == 1)
    {
      _transitionToNight = !_transitionToNight;
    }
  }
}
