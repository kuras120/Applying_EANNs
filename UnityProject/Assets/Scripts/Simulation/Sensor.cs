/// Author: Samuel Arzt
/// Date: March 2017

#region Includes

using System.Collections.Generic;
using UnityEngine;
#endregion

/// <summary>
/// Class representing a sensor reading the distance to the nearest obstacle in a specified direction.
/// </summary>
public class Sensor : MonoBehaviour
{
    #region Members
    // The layer this sensor will be reacting to, to be set in Unity editor.
    [SerializeField]
    private List<LayerMask> LayersToSense;
    //The crosshair of the sensor, to be set in Unity editor.
    [SerializeField]
    private SpriteRenderer Cross;

    // Max and min readings
    private const float MAX_DIST = 10f;
    private const float MIN_DIST = 0.01f;

    /// <summary>
    /// The current sensor readings in percent of maximum distance.
    /// </summary>
    public float Output
    {
        get;
        private set;
    }
    #endregion

    #region Constructors
    void Start ()
    {
        Cross.gameObject.SetActive(true);
	}
    #endregion

    #region Methods
    // Unity method for updating the simulation
    void FixedUpdate ()
    {
        //Calculate direction of sensor
        Vector2 direction = Cross.transform.position - transform.position;
        direction.Normalize();

        RaycastHit2D closestHit = new RaycastHit2D();
        closestHit.distance = MAX_DIST;
        foreach (var layer in LayersToSense)
        {
            //Send raycast into direction of sensor
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, MAX_DIST, layer);
            
            //Check distance
            if (hit.collider == null)
                hit.distance = MAX_DIST;
            else if (hit.distance < MIN_DIST)
                hit.distance = MIN_DIST;

            if (hit.distance < closestHit.distance)
            {
                closestHit = hit;
            }
        }

        Output = closestHit.distance; //transform to percent of max distance
        Cross.transform.position = (Vector2) transform.position + direction * closestHit.distance; //Set position of visual cross to current reading
	}

    /// <summary>
    /// Hides the crosshair of this sensor.
    /// </summary>
    public void Hide()
    {
        Cross.gameObject.SetActive(false);
    }

    /// <summary>
    /// Shows the crosshair of this sensor.
    /// </summary>
    public void Show()
    {
        Cross.gameObject.SetActive(true);
    }
    #endregion
}
