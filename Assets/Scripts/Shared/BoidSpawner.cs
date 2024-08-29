using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

public class BoidSpawner : MonoBehaviour
{
  [SerializeField] private BoidSettings boidSettings;
  [SerializeField] private BoidManager boidManager;
  [SerializeField] private Transform spawnLocation;
  [SerializeField] private CinemachineVirtualCamera mainVCam;
  [SerializeField] private CinemachineVirtualCamera fishVCam;
  private float boundaryRadius = 10;
  private Vector3 boidLocation;

    [SerializeField] private GameObject myXRRig;
    private Boid currBoid;
    private Boolean watchingFish;

    [SerializeField] public Volume volume;
    //[SerializeField] public Volume fishVolume;
    [SerializeField] private Light lightSource;
    public float duration = 0.5f;
    [SerializeField] public XRBaseController controller;
    public float Speed = 0;
    public float UpdateDelay = 1.0f;
    private Boolean recordingSpeed = false;

    private void Awake()
  {
    boidSettings.boundsOn = true;

    if (!spawnLocation)
      spawnLocation = this.transform;
    else
      boundaryRadius = spawnLocation.localScale.x / 2;
  }

  private void Start()
  {
    boidSettings.ResetSettings();
    SpawnBoids();

    if (mainVCam)
      WatchMainCamera();
    if (boidSettings.simMethod == SimMethod.Individual)
      Debug.Log("Total boids: " + Boid.boidList.Count);
    else
      Debug.Log("Total boids: " + BoidManager.BoidCount);

        lightSource.intensity = 0;
    }

  public void SpawnBoids()
  {
    if (boidSettings.nextSimMethod != boidSettings.simMethod)
      boidSettings.simMethod = boidSettings.nextSimMethod;
    Debug.Log("Spawning " + boidSettings.boidCount + " boids using method: " + boidSettings.simMethod.ToString());
    if (boidSettings.simMethod == SimMethod.Individual)
      SpawnBoidsIndividual();
    else if (boidSettings.simMethod == SimMethod.Manager)
      SpawnBoidsManager();
    else if (boidSettings.simMethod == SimMethod.MgrJobs)
      SpawnBoidsMgrJobs();
    else if (boidSettings.simMethod == SimMethod.MgrJobsECS)
      SpawnBoidsMgrJobsEcs();
  }

  private void SpawnBoidsIndividual()
  {
    // Create or clear BoidList
    if (Boid.boidList == null)
      Boid.boidList = new List<Boid>();
    else
      Boid.boidList.Clear();
    // Spawn Boids
    Boid newBoid;
    for (int i = 0; i < boidSettings.boidCount; i++)
    {
      boidLocation = UnityEngine.Random.insideUnitSphere.normalized * UnityEngine.Random.Range(0, boundaryRadius * 0.9f);
      newBoid = Instantiate(boidSettings.boidPrefab, boidLocation, Quaternion.identity, this.transform).GetComponent<Boid>();
      newBoid.boidSettings = boidSettings;
      newBoid.SetBoundarySphere(spawnLocation.position, boundaryRadius);
    }
  }

  private void SpawnBoidsManager() {
    // TODO: WRITE THIS
  }

  private void SpawnBoidsMgrJobs() {
    // TODO: WRITE THIS
  }

  private void SpawnBoidsMgrJobsEcs() {
    // TODO: WRITE THIS
  }

  public void RespawnBoids()
  {
    KillBoids();
    SpawnBoids();
    UIManager.Instance.RefreshUI();
  }

  public void KillBoids()
  {
    if (boidSettings.simMethod == SimMethod.Individual) {
      foreach (Boid boid in Boid.boidList) {
        Destroy(boid.gameObject);
      }
      Boid.boidList.Clear();
    }
    // TODO: WRITE KILL SCRIPT FOR OTHER SIM METHODS
  }

  public void ToggleCamera()
  {
    /*if (mainVCam.gameObject.activeInHierarchy)
      WatchFishCamera();
    else
      WatchMainCamera();*/
    if (watchingFish)
        WatchMainCamera();
    else
        WatchFishCamera();
    //watchingFish = !watchingFish;
  }

  public void WatchMainCamera()
  {
    /*mainVCam.gameObject.SetActive(true);
    fishVCam.gameObject.SetActive(false);*/
        watchingFish = false;
        lightSource.intensity = 0;
    }

  public void WatchFishCamera()
  {
/*    if (fishVCam)
    {
      mainVCam.gameObject.SetActive(false);
      fishVCam.gameObject.SetActive(true);
      int randomBoid = UnityEngine.Random.Range(0, boidSettings.boidCount);
            currBoid = Boid.boidList[randomBoid];
      // TODO - SET UP FOLLOW CAM FOR BOID MANAGER METHODS (ELSE CONDITION)
      if (boidSettings.simMethod == SimMethod.Individual)
        fishVCam.Follow = fishVCam.LookAt = currBoid.transform;
      else
        fishVCam.Follow = fishVCam.LookAt = currBoid.transform;

      

    }*/

        int randomBoid = UnityEngine.Random.Range(0, boidSettings.boidCount);
        currBoid = Boid.boidList[randomBoid];
        //fishVolume.weight = 1;
        watchingFish = true;
    }

    public void Update()
    {
        if (watchingFish)
        {
            Vector3 previousPosition = currBoid.transform.position;
            myXRRig.transform.position = currBoid.transform.position + new Vector3(-1,0,0);
            Vector3 currentPosition = currBoid.transform.position;
            Vector3 currentDirection = (previousPosition - currentPosition).normalized;
            myXRRig.transform.LookAt(currBoid.velocity);

            //Debug.Log(currBoid.transform.position);
            if (currBoid.getTooClose())
                StartCoroutine(TooCloseEffect());
            adjustLight();
            TriggerHaptic();
            if (!recordingSpeed)
            {
                StartCoroutine(SpeedReckoner());
                recordingSpeed = true;
            }
        }
        else
            myXRRig.transform.position = new Vector3(0, 0, 0);
    }

    private IEnumerator TooCloseEffect()
    {
        float intensity = 1f;
        //_vignette.enabled.Override(true);
        //_vignette.active = true;
        volume.weight = intensity;
        yield return new WaitForSeconds(intensity);
        while (intensity > 0)
        {
            intensity -= 0.2f;
            if (intensity < 0) intensity = 0;
            volume.weight = intensity;
            yield return new WaitForSeconds(0.1f);
        }
        //_vignette.enabled.Override(false);
        volume.weight = 0;
        yield break;
    }

    private float distanceToCenter()
    {
        Vector3 myPosition = currBoid.transform.position;
        //Debug.Log(myPosition.magnitude);
        return myPosition.magnitude;
    }

    private void adjustLight()
    {
        float lightIntensity = 100.0f - NormalizeToRange(distanceToCenter(), 3.2f, 6.7f, 0.0f, 100.0f);
        lightSource.intensity = lightIntensity;
    }
    private static float NormalizeToRange(float x, float originalMin, float originalMax, float newMin, float newMax)
    {
        // Perform the normalization calculation with floats
        float normalizedValue = ((x - originalMin) / (originalMax - originalMin)) * (newMax - newMin) + newMin;
        //Debug.Log(normalizedValue);
        return normalizedValue;
    }

    //vibration of controllers based on speed of boid 
    public void TriggerHaptic()
    {
        // float velocityMag = (float)boidSettings.speed;
        //float normalizedVelocity = (float)(( velocityMag - 0.5) / 4.5);
        float normalizedVelocity = NormalizeToRange(Speed, 0.5f, 5.0f, 0.0f, 1.0f);
        controller.SendHapticImpulse(normalizedVelocity, duration);
        Debug.Log(normalizedVelocity);
    }

    //https://stackoverflow.com/questions/55042997/how-to-calculate-a-gameobjects-speed-in-unity
    private IEnumerator SpeedReckoner()
    {

        YieldInstruction timedWait = new WaitForSeconds(UpdateDelay);
        Vector3 lastPosition = currBoid.transform.position;
        float lastTimestamp = Time.time;

        while (enabled)
        {
            yield return timedWait;

            var deltaPosition = (currBoid.transform.position - lastPosition).magnitude;
            var deltaTime = Time.time - lastTimestamp;

            if (Mathf.Approximately(deltaPosition, 0f)) // Clean up "near-zero" displacement
                deltaPosition = 0f;

            Speed = deltaPosition / deltaTime;


            lastPosition = currBoid.transform.position;
            lastTimestamp = Time.time;
        }
    }
}