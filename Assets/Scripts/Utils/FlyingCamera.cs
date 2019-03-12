using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class FlyingCamera : MonoBehaviour
    {
        /*
            Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
            Converted to C# 27-02-13 - no credit wanted.
            Simple flycam I made, since I couldn't find any others made public.  
            Made simple to use (drag and drop, done) for regular keyboard layout  
            wasd : basic movement
            shift : Makes camera accelerate
            space : Moves camera on X and Z axis only.  So camera doesn't gain any height
        */

        public float MainSpeed = 100.0f; //regular speed
        public float ShiftAdd = 250.0f; //multiplied by how long shift is held.  Basically running
        public float MaxShift = 1000.0f; //Maximum speed when holdin gshift
        public float MouseSensitivity = 0.25f; //How sensitive it with mouse
        public bool RotateOnlyIfMousedown = true;
        public bool MovementStaysFlat = true;

        private Vector3 _lastMousePosition = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float _totalRun = 1.0f;

        void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                _lastMousePosition = Input.mousePosition; // $CTK reset when we begin
            }

            if (!RotateOnlyIfMousedown || (RotateOnlyIfMousedown && Input.GetMouseButton(1)))
            {
                _lastMousePosition = Input.mousePosition - _lastMousePosition;
                _lastMousePosition = new Vector3(-_lastMousePosition.y * MouseSensitivity, _lastMousePosition.x * MouseSensitivity, 0);
                _lastMousePosition = new Vector3(transform.eulerAngles.x + _lastMousePosition.x, transform.eulerAngles.y + _lastMousePosition.y,
                    0);
                transform.eulerAngles = _lastMousePosition;
                _lastMousePosition = Input.mousePosition;
                //Mouse  camera angle done.  
            }

            var p = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            if (Input.GetKey(KeyCode.LeftShift))
            {
                _totalRun += Time.deltaTime;
                p = p * _totalRun * ShiftAdd;
                p.x = Mathf.Clamp(p.x, -MaxShift, MaxShift);
                p.y = Mathf.Clamp(p.y, -MaxShift, MaxShift);
                p.z = Mathf.Clamp(p.z, -MaxShift, MaxShift);
            }
            else
            {
                _totalRun = Mathf.Clamp(_totalRun * 0.5f, 1f, 1000f);
                p = p * MainSpeed;
            }

            transform.Translate(p * Time.deltaTime);
        }
    }
}
