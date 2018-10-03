using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsSystem : MonoBehaviour {

	public ComputeShader physicsShader; // Compute shader which works as a physics engine

	public Transform alphaObject; // Object that effects physics balls while it remains uneffected
	public GameObject ballPrefab; // Simply a ball prefab with 1-1-1 local scale values.

	public int ballCount; // Ball Count
	public float velocityRange; // Velocity range that we will randomly instantiate balls with
	public float positionRange; // Position range that we will randomly instantiate balls with
	public float gravity;



	GameObject[] ballObjects; // Ball game objects
	Vector3[] ballAccelerations;
	Vector3[] ballVelocities;
	Vector3[] ballPositions;
	float[] ballMasses;

	int kernel; // Index of main function of our compute shader.

	BallBuffers ballBuffers;

	public class BallBuffers // Buffers that we will send to shader/gpu
	{
		public ComputeBuffer accelerationBuffer;
		public ComputeBuffer velocityBuffer;
		public ComputeBuffer positionBuffer;
		public ComputeBuffer massBuffer;

		public BallBuffers (int ballCount)
		{
			accelerationBuffer = new ComputeBuffer( ballCount, 12 ); 
			velocityBuffer = new ComputeBuffer( ballCount, 12 );
			positionBuffer = new ComputeBuffer( ballCount, 12 );
			massBuffer = new ComputeBuffer( ballCount, 4 );
		}
	};

	
	void InstantiateBalls()
	{
		ballAccelerations = new Vector3[ballCount];
		ballVelocities = new Vector3[ ballCount ];
		ballPositions = new Vector3[ ballCount ];
		ballMasses = new float[ ballCount ];
		ballObjects = new GameObject[ ballCount ];


		for( int i = 0 ; i < ballCount ; i++ ) // Set the variables of ball arrays
		{
			ballAccelerations[ i ] = Vector3.zero;
			ballVelocities[ i ] = new Vector3( Random.Range( -velocityRange, velocityRange ), Random.Range( -velocityRange, velocityRange ), Random.Range( -velocityRange, velocityRange ) );
			ballPositions[ i ] = new Vector3( Random.Range( -positionRange, positionRange ), Random.Range( -positionRange, positionRange ), Random.Range( -positionRange, positionRange ) );

			ballObjects[ i ] = Instantiate( ballPrefab, ballPositions[ i ], Quaternion.identity ) as GameObject; 
		}

	}


	// Use this for initialization
	void Start()
	{
		InstantiateBalls(); // Sets random Velocities, Positions and Gameobjects

		kernel = physicsShader.FindKernel( "CSMain" );
		ballBuffers = new BallBuffers( ballCount );

		ballBuffers.accelerationBuffer.SetData( ballAccelerations );
		ballBuffers.velocityBuffer.SetData( ballVelocities );
		ballBuffers.positionBuffer.SetData( ballPositions );
		ballBuffers.massBuffer.SetData( ballMasses );

		physicsShader.SetBuffer( kernel, "PUT VARIABLE NAME HERE", ballBuffers.accelerationBuffer );
		physicsShader.SetBuffer( kernel, "PUT VARIABLE NAME HERE", ballBuffers.velocityBuffer );
		physicsShader.SetBuffer( kernel, "PUT VARIABLE NAME HERE", ballBuffers.positionBuffer );
		physicsShader.SetBuffer( kernel, "PUT VARIABLE NAME HERE", ballBuffers.massBuffer );

		UpdateShaderValues();


	}

	void UpdateShaderValues()
	{
		physicsShader.SetFloat( "PUT VARIABLE NAME HERE", gravity );  // Strangely primitive data types doesnt need kernel. I don't know why.
		physicsShader.SetVector( "PUT VARIABLE NAME HERE", alphaObject.position );

	}

	// Update is called once per frame
	void Update () {
		 




		}

	void RunComputeShader()
	{
		physicsShader.Dispatch( kernel, ballCount, 1, 1 );
		


	}

}
