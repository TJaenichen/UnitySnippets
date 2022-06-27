using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Verlet
{
    public class VerletSolverByArrays : MonoBehaviour
    {
       

        private Transform[] Transforms;
        private Vector3[] CurrentPositions;
        private Vector3[] LastPositions;
        private Vector3[] Accelerations;
        private float[] Radii;
        
        
        public double SubSteps;
        public Vector3 Gravity;
        public GameObject VerletSphere;
        
        public int NumObjects;

        public Vector3 ConstraintCenter = new(0, 50, 0);

        public float ConstraintMaxDistance = 50;

        private float _currentBankAngle;
        private float _desiredBankAngle;

        private Gradient _gradient;
        
        void Start()
        {
            Transforms = new Transform[NumObjects];
            CurrentPositions = new Vector3[NumObjects];
            LastPositions = new Vector3[NumObjects];
            Accelerations = new Vector3[NumObjects];
            Radii = new float[NumObjects];
            
            _gradient = new Gradient();
            var colorKey = new GradientColorKey[3];
            colorKey[0].color = Color.red;
            colorKey[0].time = 0.0f;
            colorKey[1].color = Color.green;
            colorKey[1].time = 0.5f;
            colorKey[2].color = Color.blue;
            colorKey[2].time = 1.0f;

            // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
            var alphaKey = new GradientAlphaKey[3];
            alphaKey[0].alpha = 1.0f;
            alphaKey[0].time = 0.0f;
            alphaKey[1].alpha = 1.0f;
            alphaKey[1].time = 0.5f;
            alphaKey[2].alpha = 1.0f;
            alphaKey[2].time = 1.0f;

            _gradient.colorKeys = colorKey;
            _gradient.alphaKeys = alphaKey;
            
            StartCoroutine(Spawn());
        }

        private int Spawned = 0;
        IEnumerator Spawn()
        {
            while (Spawned < NumObjects)
            {
                if (Spawned == 0)
                {
                    SpawnOne();
                    yield return null;
                }
                if(Vector3.Distance(CurrentPositions[Spawned-1], transform.position) < 5)
                {
                    yield return null;
                    continue;
                }
                SpawnOne();
            }
        }

        public float duration = 3;
        
        void SpawnOne()
        {
            var go = Instantiate(VerletSphere);
            
            float MaxAngleDeflection = 50.0f;
            float SpeedOfPendulum = 1.0f;
 
            float sinAngle = MaxAngleDeflection * Mathf.Sin( Time.time * SpeedOfPendulum);
            float cosAngle = MaxAngleDeflection * Mathf.Cos( Time.time * SpeedOfPendulum);
            transform.localRotation = Quaternion.Euler( cosAngle, 0, sinAngle);
            
            var pos = transform.position;

            CurrentPositions[Spawned] = pos;
            LastPositions[Spawned] = pos;
            Transforms[Spawned] = go.transform;
            Radii[Spawned] = Random.value * 5;
           
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * Radii[Spawned];
            
            go.GetComponent<MeshRenderer>().material.color = _gradient.Evaluate(Mathf.PingPong(Time.time, duration) / duration);
            
            LastPositions[Spawned] -= transform.TransformDirection(Vector3.down * 2);
            Spawned++;
        }
        
        void Update()
        {
            UpdateVerlet(Time.deltaTime);        
        }

        void UpdateVerlet(float deltaTime)
        {
            var subDelta = deltaTime / SubSteps;
            for (var i = 0; i < subDelta; i++)
            {
                ApplyGravity();
                ApplyConstraint();
                SolveCollisions();
                UpdatePositions(deltaTime);    
            }
        }

        void UpdatePositions(float deltaTime)
        {
            for(var i = 0; i < Spawned; i++)
            {
                UpdatePosition(deltaTime, i);
            }
        }
        
        void ApplyGravity()
        {
            for(var i = 0; i < Spawned; i++)
            {
                Accelerate(Gravity, i);
            }
        }

        void ApplyConstraint()
        {
            for(var i = 0; i < Spawned; i++)
            {
                var toObj = CurrentPositions[i] - ConstraintCenter;
                var distance = toObj.magnitude;
                if (distance > ConstraintMaxDistance - Radii[i] / 2)
                {
                    var n = toObj / distance;
                    CurrentPositions[i] = ConstraintCenter + n * (ConstraintMaxDistance - Radii[i] / 2);
                }
            }
        }

        void SolveCollisions()
        {
            Parallel.For(0, Spawned, i =>
            {
                var h = 0;
                for (var j = i + 1; j < Spawned; j++)
                {
                    var collisionAxis = CurrentPositions[i] - CurrentPositions[j];
                    var distance = Vector3.Magnitude(collisionAxis);
                    var minDistance = (Radii[i] / 2 + Radii[j] / 2);
                    if (distance < minDistance)
                    {
                        var n = collisionAxis / distance;
                        var delta = minDistance - distance;
                        CurrentPositions[i] += .5f * delta * n;
                        CurrentPositions[j] -= .5f * delta * n;
                    }
                }
            });
        }
        
         
        public void UpdatePosition(float deltaTime, int index)
        {
            var velocity = CurrentPositions[index] - LastPositions[index];
            LastPositions[index] = CurrentPositions[index];
            CurrentPositions[index] = CurrentPositions[index] + velocity + Accelerations[index] * (deltaTime * deltaTime);
            Accelerations[index] = new Vector3();
            Transforms[index].position = CurrentPositions[index];
        }

        public void Accelerate(Vector3 acceleration, int index)
        {
            Accelerations[index] += acceleration;
        }
    }
}
