using UnityEngine;

public class Pajaro : MonoBehaviour
{
    private Animator animator;

    [SerializeField] string estado;

    [SerializeField] float distanciaMaxima = 30f;
    [SerializeField] float alturaDeVuelo = 20f;
    private Vector3 proximoPunto;

    private Transform playerTransform;
    private Rigidbody rigidBody;
    private float timeOutPunto = 0f;


    private void Start() {
        animator = gameObject.GetComponentInChildren<Animator>();
        playerTransform = GameObject.FindWithTag("Player").transform;
        rigidBody = gameObject.GetComponent<Rigidbody>();


        estado = "idle";

        InvokeRepeating("SlowUpdate", 1f, 10f);

        GenerarProximoPunto();
    }

    private void Update() {
        switch (estado)
        {
            case "idle":
                if (Vector3.Distance(transform.position, proximoPunto) < 4f)
                {
                    GenerarProximoPunto();
                }

                break;

            case "walk":

                Volar();

                if (Vector3.Distance(transform.position, proximoPunto) < 4f)
                {
                    timeOutPunto = 0f;
                    GenerarProximoPunto();
                }

                break;

            case "fly":

                Volar();

                if (Vector3.Distance(transform.position, proximoPunto) < 4f)
                {
                    timeOutPunto = 0f;
                    GenerarProximoPunto();
                }

                break;
        }
    }

    private void SlowUpdate()
    {
        if (Random.Range(0f, 100f) > 10f) //Fly
        {
            estado = "fly";
            rigidBody.useGravity = false;
            
            animator.SetBool("fly", true);
            animator.SetBool("walk", false);
        }
        else if (Random.Range(0f, 100f) > 60f) //Idle
        {
            estado = "idle";
            rigidBody.useGravity = true;

            animator.SetBool("fly", false);
            animator.SetBool("walk", false);
        }
        else if (Random.Range(0f, 100f) > 60f) //Walk
        {
            estado = "walk";
            rigidBody.useGravity = true;

            animator.SetBool("fly", false);
            animator.SetBool("walk", true);
        }

        if (Vector3.Distance(transform.position, playerTransform.position) > 100)
        {
            transform.position = new Vector3(playerTransform.position.x, alturaDeVuelo, playerTransform.position.z);
        }
    }

    private void GenerarProximoPunto()
    {
        float x = transform.position.x + Random.Range(-distanciaMaxima, distanciaMaxima);
        float z = transform.position.z + Random.Range(-distanciaMaxima, distanciaMaxima);

        if (estado == "walk")
        {
            proximoPunto = new Vector3(x, transform.position.y, z);
        } 
        else
        {
            proximoPunto = new Vector3(x, alturaDeVuelo, z);
        }

        
    }

    private void Volar()
    {
        Vector3 direccion = (proximoPunto - transform.position).normalized;
        rigidBody.AddForce(direccion * 1f, ForceMode.Force);

        Vector3 mirar = rigidBody.linearVelocity;

        if (mirar != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(mirar);
        }

        if (rigidBody.linearVelocity.magnitude > 6f)
        {
            rigidBody.linearVelocity = rigidBody.linearVelocity.normalized * 6f;
        }

        timeOutPunto += Time.deltaTime;

        if (timeOutPunto >= 5f)
        {
            GenerarProximoPunto();
            timeOutPunto = 0f;
        }
    }
}