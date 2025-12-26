using UnityEngine;
using System;

public enum SoundType
{
    DISPARO,
    MAGIA,
    MOVIMIENTO,
    EFECTOS_ESPECIALES,
    INTERACCION,
    DANO_PROTAGONISTA,
    DANO_ENEMIGO,
    PASOS
}

[RequireComponent(typeof(AudioSource)), ExecuteInEditMode]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;
    private static SoundManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1.0f)
    {
        /*
        AudioClip[] clips = instance.soundList[(int)sound].Sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume);
        */

        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref soundList, names.Length);
        for(int i = 0; i < names.Length; i++)
        {
            soundList[i].name = names[i];
        }
    }
#endif
}

[Serializable]
public struct SoundList
{
    public AudioClip[] Sounds { get => sounds; }
    /*[SerializeField]*/ [HideInInspector] public string name;
    [SerializeField] private AudioClip[] sounds;
}
