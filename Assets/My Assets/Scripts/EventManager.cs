using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;
    
    // Event fired when parrot is destroyed
    public static System.Action OnParrotDestroyed;
    public static System.Action OnEnemyShip1Arrive;
    public static System.Action OnEnemyShip2Arrive;
    public static System.Action OnEnemyShip3Arrive;
    public static System.Action OnEnemyShip4Arrive;
    
    // Event fired to start weather system
    public static System.Action OnStartWeatherSystem;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnParrotDestroyedApplier(){
        OnParrotDestroyed?.Invoke();
    }
    
    public void StartWeatherSystem(){
        OnStartWeatherSystem?.Invoke();
    }

    public void OnEnemyShip1ArriveApplier(){
        OnEnemyShip1Arrive?.Invoke();
    }

    public void OnEnemyShip2ArriveApplier(){
        OnEnemyShip2Arrive?.Invoke();
    }
    
    public void OnEnemyShip3ArriveApplier(){
        OnEnemyShip3Arrive?.Invoke();
    }
    
    public void OnEnemyShip4ArriveApplier(){
        OnEnemyShip4Arrive?.Invoke();
    }
}
