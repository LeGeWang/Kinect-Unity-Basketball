﻿using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System.Linq;

public class Shoot : MonoBehaviour
{
    public GameObject ball;
    private Vector3 throwSpeed = new Vector3(0, 26, 40); //This value is a sure basket
    public Vector3 ballPos;
    private bool thrown = false;
    private GameObject ballClone;

    public GameObject availableShotsGO;
    private int availableShots = 5;

    public GameObject meter;
    public GameObject arrow;
    private float arrowSpeed = 0.3f; //Difficulty
    private bool right = true;

    public GameObject gameOver;

    private KinectSensor _sensor;
    private BodyFrameReader _bodyFrameReader;
    private Body[] _bodies = null;

    public GameObject kinectAvailableText;
    //public Text handXText;

    public bool IsAvailable;
    public float PaddlePosition;
    public bool IsFire;

    public static Shoot instance = null;

    public Body[] GetBodies()
    {
        return _bodies;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
            Destroy(gameObject);
    }

    // Use this for initialization
    void Start()
    {
        /* Increase Gravity */
        Physics.gravity = new Vector3(0, -20, 0);

        _sensor = KinectSensor.GetDefault();

        if (_sensor != null)
        {
            IsAvailable = _sensor.IsAvailable;

            //kinectAvailableText.SetActive(IsAvailable);

            _bodyFrameReader = _sensor.BodyFrameSource.OpenReader();

            if (!_sensor.IsOpen)
            {
                _sensor.Open();
            }

            _bodies = new Body[_sensor.BodyFrameSource.BodyCount];
        }
    }


    public int counter_cooldown = 0;
    void FixedUpdate()
    {
        counter_cooldown--; // delay the amount of times you can shoot to once per movement
        IsAvailable = _sensor.IsAvailable;

        if (_bodyFrameReader != null)
        {
            var frame = _bodyFrameReader.AcquireLatestFrame();

            if (frame != null)
            {
                frame.GetAndRefreshBodyData(_bodies);

                foreach (var body in _bodies.Where(b => b.IsTracked))
                {
                    IsAvailable = true;
                    
                   

                    /* get instances of the elbow and shoulders and head*/
                    /*  insert left elbow and shoulder here for 2 hand throw*/
                    Windows.Kinect.Joint elbow = body.Joints[JointType.ElbowLeft];
                    Windows.Kinect.Joint shoulder = body.Joints[JointType.ShoulderLeft];
             
                    Windows.Kinect.Joint hand = body.Joints[JointType.HandLeft];
                    // if the right elbow goes above the right shoulder the power meter becomes
                    // available 
                    if (elbow.Position.Y > shoulder.Position.Y) {
                        /* Move Meter Arrow */

                        if (arrow.transform.position.x < 4.7f && right)
                        {
                            arrow.transform.position += new Vector3(arrowSpeed, 0, 0);
                        }
                        if (arrow.transform.position.x >= 4.7f)
                        {
                            right = false;
                        }
                        if (right == false)
                        {
                            arrow.transform.position -= new Vector3(arrowSpeed, 0, 0);
                        }
                        if (arrow.transform.position.x <= -4.7f)
                        {
                            right = true;
                        }
                        if (body.HandLeftState == HandState.Open && (counter_cooldown <= 0) && availableShots > 0)
                        {
                            counter_cooldown = 100; // delay next available shot
                            thrown = true;
                            availableShots--;
                            availableShotsGO.GetComponent<GUIText>().text = availableShots.ToString();

                            ballClone = Instantiate(ball, ballPos, transform.rotation) as GameObject;
                            //throwSpeed.y = throwSpeed.y + arrow.transform.position.x;
                            //throwSpeed.z = throwSpeed.z + arrow.transform.position.x;
                            // justification for following code.
                            // because the ball can only be thrown when your elbow is above your shoulder
                            // the only way to move your hand closer to your shoulder would be to bend your elbow back
                            // in a throwing motion
                            // hence the throw power is determined by the distance between the height of the hand and
                            // height of the shoulder
                            throwSpeed.y = throwSpeed.y + (hand.Position.Y - shoulder.Position.Y) * 2;
                            throwSpeed.z = throwSpeed.z + (hand.Position.Y - shoulder.Position.Y) * 2;
                            ballClone.GetComponent<Rigidbody>().AddForce(throwSpeed, ForceMode.Impulse);
                            

                        }
                    }
                    

                    /* Remove Ball when it hits the floor */

                    if (ballClone != null && ballClone.transform.position.y < -16)
                    {
                        Destroy(ballClone);
                        thrown = false;
                        throwSpeed = new Vector3(0, 26, 40);//Reset perfect shot variable

                        /* Check if out of shots */

                        if (availableShots == 0)
                        {
                            arrow.GetComponent<Renderer>().enabled = false;
                            Instantiate(gameOver, new Vector3(0.31f, 0.2f, 0), transform.rotation);
                            Invoke("restart", 2);
                        }
                    }
                }

                frame.Dispose();
                frame = null;
            }
        }

        
    }
    

    void restart()
    {
        Application.LoadLevel(Application.loadedLevel);
    }

    static float RescalingToRangesB(float scaleAStart, float scaleAEnd, float scaleBStart, float scaleBEnd, float valueA)
    {
        return (((valueA - scaleAStart) * (scaleBEnd - scaleBStart)) / (scaleAEnd - scaleAStart)) + scaleBStart;
    }

    void OnApplicationQuit()
    {
        if (_bodyFrameReader != null)
        {
            _bodyFrameReader.IsPaused = true;
            _bodyFrameReader.Dispose();
            _bodyFrameReader = null;
        }

        if (_sensor != null)
        {
            if (_sensor.IsOpen)
            {
                _sensor.Close();
            }

            _sensor = null;
        }
    }
}