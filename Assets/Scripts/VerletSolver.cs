using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Verlet
{
    public class VerletSolver : MonoBehaviour
    {
        public class VerletObject
        {
            public Transform Transform;
            public Vector3 CurrentPosition;
            public Vector3 LastPosition;
            public Vector3 Acceleration;
            public float Radius;
            
            public void UpdatePosition(float deltaTime)
            {
                var velocity = CurrentPosition - LastPosition;
                LastPosition = CurrentPosition;
                CurrentPosition = CurrentPosition + velocity + Acceleration * (deltaTime * deltaTime);
                Acceleration = new Vector3();
                Transform.position = CurrentPosition;
            }

            public void Accelerate(Vector3 acceleration)
            {
                Acceleration += acceleration;
            }
        }

        public double SubSteps;
        public List<VerletObject> VerletObjects = new();
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

        IEnumerator Spawn()
        {
            while (VerletObjects.Count < NumObjects)
            {
                if (VerletObjects.Count == 0)
                {
                    SpawnOne();
                    yield return null;
                }
                if(Vector3.Distance(VerletObjects[^1].CurrentPosition, transform.position) < 5)
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
            var verlet = new VerletObject
            {
                CurrentPosition = pos,
                LastPosition = pos,
                Transform = go.transform,
                Radius = Random.value * 5
            };
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * verlet.Radius;
            
            go.GetComponent<MeshRenderer>().material.color = _gradient.Evaluate(Mathf.PingPong(Time.time, duration) / duration);
            
            VerletObjects.Add(verlet);
            verlet.LastPosition -= transform.TransformDirection(Vector3.down * 2);
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
            foreach (var verletObject in VerletObjects)
            {
                verletObject.UpdatePosition(deltaTime);
            }
        }
        
        void ApplyGravity()
        {
            foreach (var verletObject in VerletObjects)
            {
                verletObject.Accelerate(Gravity);
            }
        }

        void ApplyConstraint()
        {
            foreach (var verletObject in VerletObjects)
            {
                var toObj = verletObject.CurrentPosition - ConstraintCenter;
                var distance = toObj.magnitude;
                if (distance > ConstraintMaxDistance - verletObject.Radius / 2)
                {
                    var n = toObj / distance;
                    verletObject.CurrentPosition = ConstraintCenter + n * (ConstraintMaxDistance - verletObject.Radius / 2);
                }
            }
        }

        void SolveCollisions()
        {
            var objectCount = VerletObjects.Count;

            Parallel.For(0, objectCount, i =>
            {
                var verlet1 = VerletObjects[i];
                for (var j = i + 1; j < objectCount; j++)
                {
                    var verlet2 = VerletObjects[j];
                    var collisionAxis = verlet1.CurrentPosition - verlet2.CurrentPosition;
                    var distance = Vector3.Magnitude(collisionAxis);
                    var minDistance = (verlet1.Radius / 2 + verlet2.Radius / 2);
                    if (distance < minDistance)
                    {
                        var n = collisionAxis / distance;
                        var delta = minDistance - distance;
                        verlet1.CurrentPosition += .5f * delta * n;
                        verlet2.CurrentPosition -= .5f * delta * n;
                    }
                }
            });
        }
    }
}
