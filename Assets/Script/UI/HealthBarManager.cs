using System.Collections.Generic;
using UnityEngine;

public class HealthBarManager : MonoBehaviour
{
    public static HealthBarManager Instance;

    public CarHealthBar healthBarPrefab;
    public Canvas canvas;

    private readonly Dictionary<CarHealth, CarHealthBar> bars = new Dictionary<CarHealth, CarHealthBar>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CarHealth[] allCars = FindObjectsByType<CarHealth>(FindObjectsSortMode.None);
        foreach (CarHealth car in allCars)
        {
            RegisterCar(car);
        }
    }

    public void RegisterCar(CarHealth car)
    {
        if (car == null || healthBarPrefab == null || canvas == null)
            return;

        if (bars.ContainsKey(car))
            return;

        CarHealthBar hb = Instantiate(healthBarPrefab, canvas.transform);
        hb.carHealth = car;
        hb.target = car.transform;
        hb.SetFillColor(Color.red);

        bars.Add(car, hb);
        car.OnDied += OnCarDied;
    }

    public void UnregisterCar(CarHealth car)
    {
        if (car == null)
            return;

        car.OnDied -= OnCarDied;

        if (bars.TryGetValue(car, out CarHealthBar hb))
        {
            if (hb != null)
                Destroy(hb.gameObject);

            bars.Remove(car);
        }
    }

    void OnCarDied(CarHealth car)
    {
        UnregisterCar(car);
    }
}
