using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// A base class used to spawn objects at a given rate over time.
    /// To create an object spawner, inherit this class and override the Spawn function with your desired spawn logic, e.g. Instantiate(prefab)
    /// The spawner will call the spawn function at the given spawn rate automatically.
    /// </summary>
    public abstract class ObjectSpawner : MonoBehaviour
    {
        /// <summary>
        /// The frequency at which objects are spawned (number of objects per second).
        /// </summary>
        [Tooltip("The frequency at which objects are spawned (number of objects per second).")]
        public float spawnRate;
        float timer;

        // Start is called before the first frame update
        void Start()
        {
            //@Conor giving this as an option is baffling behaviour for the user to have to understand in my opinion. Surely the first event at t=0 should be a spawn?
            //Spawn the first item at t=0
            Spawn();

        }

        /// <summary>
        /// When overriding this fixed update make sure to call base.FixedUpdate() so that the spawn timer still works.
        /// </summary>
        public virtual void FixedUpdate()
        {
            if (spawnRate <= 0f)
            {
                return;
            }

            timer += Time.fixedDeltaTime;
            if (timer > 1f / spawnRate)
            {
                Spawn();
                timer -= 1f / spawnRate;
            }
        }

        /// <summary>
        /// Define the logic for creating/spawning the object.
        /// </summary>
        public abstract void Spawn();
    }
}