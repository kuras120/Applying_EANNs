/// Author: Samuel Arzt
/// Date: March 2017

#region Includes
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Simulation;

#endregion

/// <summary>
/// Singleton class managing the current track and all cars racing on it, evaluating each individual.
/// </summary>
public class TrackManager : MonoBehaviour
{
    #region Members
    public static TrackManager Instance
    {
        get;
        private set;
    }

    // Sprites for visualising best and second best cars. To be set in Unity Editor.
    [SerializeField]
    private Sprite BestCarSprite;
    [SerializeField]
    private Sprite SecondBestSprite;
    [SerializeField]
    private Sprite NormalCarSprite;

    /// <summary>
    /// Car used to create new cars and to set start position.
    /// </summary>
    public CarCollection[] CarCollections;

    /// <summary>
    /// The amount of cars currently on the track.
    /// </summary>
    public int CarCount(uint index)
    {
        return CarCollections[index].Cars.Count;
    }

    #region Best and Second best

    /// <summary>
    /// The current best car (furthest in the track).
    /// </summary>
    public CarController BestCar(uint index)
    {
        return CarCollections[index].FirstbestCar;
    }

    private void BestCar(uint index, CarController car) 
    {
        if (CarCollections[index].FirstbestCar != car)
        {
            //Update appearance
            if (BestCar(index) != null)
                BestCar(index).SpriteRenderer.sprite = NormalCarSprite;
            if (car != null)
                car.SpriteRenderer.sprite = BestCarSprite;

            //Set previous best to be second best now
            CarController previousBest = CarCollections[index].FirstbestCar;
            CarCollections[index].FirstbestCar = car;
            if (BestCarChanged != null)
                BestCarChanged(CarCollections[index].FirstbestCar);

            SecondBestCar(index, previousBest);
        }
    }
    /// <summary>
    /// Event for when the best car has changed.
    /// </summary>
    public event Action<CarController> BestCarChanged;
    
    /// <summary>
    /// The current second best car (furthest in the track).
    /// </summary>
    public CarController SecondBestCar(uint index)
    {
        return CarCollections[index].SecondBestCar;
    }
    private void SecondBestCar(uint index, CarController car)
    {
        if (SecondBestCar(index) != car)
        {
            //Update appearance of car
            if (SecondBestCar(index) != null && SecondBestCar(index) != BestCar(index))
                SecondBestCar(index).SpriteRenderer.sprite = NormalCarSprite;
            if (car != null)
                car.SpriteRenderer.sprite = SecondBestSprite;

            CarCollections[index].SecondBestCar = car;
            if (SecondBestCarChanged != null)
                SecondBestCarChanged(SecondBestCar(index));
        }
    }
    /// <summary>
    /// Event for when the second best car has changed.
    /// </summary>
    public event Action<CarController> SecondBestCarChanged;
    #endregion
    
    /// <summary>
    /// The length of the current track in Unity units (accumulated distance between successive checkpoints).
    /// </summary>
    public float TrackLength(uint index)
    {
        return CarCollections[index].TrackLength;
    }

    public void TrackLength(uint index, float length)
    {
        CarCollections[index].TrackLength = length;
    }
    #endregion

    #region Constructors
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Mulitple instance of TrackManager are not allowed in one Scene.");
            return;
        }

        Instance = this;
        
        for (uint iter = 0; iter < CarCollections.Length; iter++)
        {
            //Get all checkpoints
            CarCollections[iter].Checkpoints = CarCollections[iter].GetComponentsInChildren<Checkpoint>();
            
            //Set start position and hide prototype
            var prototypeTransform = CarCollections[iter].CarController.transform;
            CarCollections[iter].StartPosition = prototypeTransform.position;
            CarCollections[iter].StartRotation = prototypeTransform.rotation;
            CarCollections[iter].CarController.gameObject.SetActive(false);
            
            CalculateCheckpointPercentages(iter);
        }
    }

    void Start()
    {
        //Hide checkpoints
        foreach (var collection in CarCollections)
        {
            foreach (Checkpoint check in collection.Checkpoints)
                check.IsVisible = false;
        }
    }
    #endregion

    #region Methods
    // Unity method for updating the simulation
    void Update()
    {
        for (uint iter = 0; iter < CarCollections.Length; iter++)
        {
            //Update reward for each enabled car on the track
            for (int i = 0; i < CarCollections[iter].Cars.Count; i++)
            {
                RaceCar car = CarCollections[iter].Cars[i];
                if (car.Car.enabled)
                {
                    car.Car.CurrentCompletionReward = GetCompletePerc(car.Car, ref car.CheckpointIndex, iter);

                    //Update best
                    if (BestCar(iter) == null || car.Car.CurrentCompletionReward >= BestCar(iter).CurrentCompletionReward)
                        BestCar(iter, car.Car);
                    else if (SecondBestCar(iter) == null || car.Car.CurrentCompletionReward >= SecondBestCar(iter).CurrentCompletionReward)
                        SecondBestCar(iter, car.Car);
                }
            }   
        }
    }

    public void SetCarAmount(int amount)
    {
        //Check arguments
        if (amount < 0) throw new ArgumentException("Amount may not be less than zero.");

        foreach (var collection in CarCollections)
        {
            if (amount == collection.Cars.Count) continue;

            if (amount > collection.Cars.Count)
            {
                //Add new cars
                for (int toBeAdded = amount - collection.Cars.Count; toBeAdded > 0; toBeAdded--)
                {
                    GameObject carCopy = Instantiate(collection.CarController.gameObject);
                    carCopy.transform.position = collection.StartPosition;
                    carCopy.transform.rotation = collection.StartRotation;
                    CarController controllerCopy = carCopy.GetComponent<CarController>();
                    collection.Cars.Add(new RaceCar(controllerCopy, 1));
                    carCopy.SetActive(true);
                }
            }
            else if (amount < collection.Cars.Count)
            {
                //Remove existing cars
                for (int toBeRemoved = collection.Cars.Count - amount; toBeRemoved > 0; toBeRemoved--)
                {
                    RaceCar last = collection.Cars[collection.Cars.Count - 1];
                    collection.Cars.RemoveAt(collection.Cars.Count - 1);

                    Destroy(last.Car.gameObject);
                }
            }   
        }
    }

    /// <summary>
    /// Restarts all cars and puts them at the track start.
    /// </summary>
    public void Restart()
    {
        foreach (var collection in CarCollections)
        {
            foreach (RaceCar car in collection.Cars)
            {
                var carTransform = car.Car.transform;
                carTransform.position = collection.StartPosition;
                carTransform.rotation = collection.StartRotation;
                car.Car.Restart();
                car.CheckpointIndex = 1;
            }

            collection.FirstbestCar = null;
            collection.SecondBestCar = null;   
        }
    }

    /// <summary>
    /// Returns an Enumerator for iterator through all cars currently on the track.
    /// </summary>
    public IEnumerator<CarController> GetCarEnumerator(uint index)
    {
        for (int i = 0; i < CarCount(index); i++)
            yield return CarCollections[index].Cars[i].Car;
    }

    /// <summary>
    /// Calculates the percentage of the complete track a checkpoint accounts for. This method will
    /// also refresh the <see cref="TrackLength"/> property.
    /// </summary>
    private void CalculateCheckpointPercentages(uint index)
    {
        CarCollections[index].Checkpoints[0].AccumulatedDistance = 0; //First checkpoint is start
        //Iterate over remaining checkpoints and set distance to previous and accumulated track distance.
        for (int i = 1; i < CarCollections[index].Checkpoints.Length; i++)
        {
            CarCollections[index].Checkpoints[i].DistanceToPrevious = Vector2.Distance(CarCollections[index].Checkpoints[i].transform.position, CarCollections[index].Checkpoints[i - 1].transform.position);
            CarCollections[index].Checkpoints[i].AccumulatedDistance = CarCollections[index].Checkpoints[i - 1].AccumulatedDistance + CarCollections[index].Checkpoints[i].DistanceToPrevious;
        }

        //Set track length to accumulated distance of last checkpoint
        TrackLength(index, CarCollections[index].Checkpoints[CarCollections[index].Checkpoints.Length - 1].AccumulatedDistance);
        
        //Calculate reward value for each checkpoint
        for (int i = 1; i < CarCollections[index].Checkpoints.Length; i++)
        {
            CarCollections[index].Checkpoints[i].RewardValue = (CarCollections[index].Checkpoints[i].AccumulatedDistance / TrackLength(index)) - CarCollections[index].Checkpoints[i-1].AccumulatedReward;
            CarCollections[index].Checkpoints[i].AccumulatedReward = CarCollections[index].Checkpoints[i - 1].AccumulatedReward + CarCollections[index].Checkpoints[i].RewardValue;
        }
    }

    // Calculates the completion percentage of given car with given completed last checkpoint.
    // This method will update the given checkpoint index accordingly to the current position.
    private float GetCompletePerc(CarController car, ref uint curCheckpointIndex, uint index)
    {
        //Already all checkpoints captured
        if (curCheckpointIndex >= CarCollections[index].Checkpoints.Length)
            return 1;

        //Calculate distance to next checkpoint
        float checkPointDistance = Vector2.Distance(car.transform.position, CarCollections[index].Checkpoints[curCheckpointIndex].transform.position);

        //Check if checkpoint can be captured
        if (checkPointDistance <= CarCollections[index].Checkpoints[curCheckpointIndex].CaptureRadius)
        {
            curCheckpointIndex++;
            car.CheckpointCaptured(); //Inform car that it captured a checkpoint
            return GetCompletePerc(car, ref curCheckpointIndex, index); //Recursively check next checkpoint
        }
        else
        {
            //Return accumulated reward of last checkpoint + reward of distance to next checkpoint
            return CarCollections[index].Checkpoints[curCheckpointIndex - 1].AccumulatedReward + CarCollections[index].Checkpoints[curCheckpointIndex].GetRewardValue(checkPointDistance);
        }
    }
    #endregion

}
