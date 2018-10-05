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
	public float gravity; // Gravitational Force
	public float borderForce; // The force that applies balls that are beyond the border
	public float frictionRate; // Friction rate is for to prevent constant acceleration
	public float alphaMass; // Mass of the alpha ball, consider this as radius size instead of mass (Miss-named)

	// Border positions, x value of the vector must be below zero and y value of the vector must be greater than zero.
	public Vector2 xRange; 
	public Vector2 yRange; 
	public Vector2 zRange;

	public Vector2 massRange; // X value minimum, Y value maximum mass.

	// Ball Value Arrays
	GameObject[] ballObjects; // Ball game objects
	Vector3[] ballAccelerations; 
	Vector3[] ballVelocities;
	Vector3[] ballPositions;
	Vector3[] ballResult;
	float[] ballMasses;

	BallBuffers ballBuffers; // Buffer that we will send to GPU.


	int mainKernel; // Index of main function of our compute shader.
	int setPosKernel;

	public class BallBuffers // Buffers that we will send to shader/gpu
	{
		// All the buffers that we need.
		public ComputeBuffer accelerationBuffer;
		public ComputeBuffer velocityBuffer;
		public ComputeBuffer positionBuffer;
		public ComputeBuffer massBuffer;
		public ComputeBuffer resultBuffer;

		public BallBuffers (int ballCount)
		{
			/* Second part of the parameter takes the size of the data type. For example
			/ Vector3D has 3 float values. 1 Float size equals to 4 bytes or 32 bits. 3 float
			values equals to 4*3 = 12 bytes. */
			accelerationBuffer = new ComputeBuffer( ballCount, 12 ); 
			velocityBuffer = new ComputeBuffer( ballCount, 12 );
			positionBuffer = new ComputeBuffer( ballCount, 12 );
			resultBuffer = new ComputeBuffer( ballCount, 12 );
			massBuffer = new ComputeBuffer( ballCount, 4 ); // Mass buffer is not a vector but single float value. So it's size is 4 bytes.
		}
	};

	
	void InstantiateBalls() // Create and initiate balls
	{
		ballAccelerations = new Vector3[ballCount];
		ballVelocities = new Vector3[ ballCount ];
		ballPositions = new Vector3[ ballCount ];
		ballResult = new Vector3[ ballCount ];
		ballMasses = new float[ ballCount ];
		ballObjects = new GameObject[ ballCount ];
		

		for( int i = 0 ; i < ballCount ; i++ ) // Set the variables of ball arrays
		{

			ballAccelerations[ i ] = Vector3.zero;

			ballVelocities[ i ] = new Vector3( Random.Range( -velocityRange, velocityRange ), Random.Range( -velocityRange, velocityRange ), Random.Range( -velocityRange, velocityRange ) );

			ballPositions[ i ] = new Vector3( Random.Range( -positionRange, positionRange ), Random.Range( -positionRange, positionRange ), Random.Range( -positionRange, positionRange ) );

			ballMasses[ i ] = Random.Range(massRange.x, massRange.y);

			ballObjects[ i ] = Instantiate( ballPrefab, ballPositions[ i ], Quaternion.identity ) as GameObject;
			ballObjects[ i ].transform.localScale = new Vector3( ballMasses[ i ], ballMasses[ i ], ballMasses[ i ] );

		
		}

	}


	// Use this for initialization
	void Start()
	{
		// Finds function indexes that we want to execute in gpu
		mainKernel = physicsShader.FindKernel( "CSMain" ); 
		setPosKernel = physicsShader.FindKernel( "setPositions" ); 

		physicsShader.SetInt( "length", ballCount );
		InstantiateBalls(); // Sets random Velocities, Positions and Gameobjects

		ballBuffers = new BallBuffers( ballCount );

		// Sets data of buffers. (Basically we pass our arrays, i don't know if it uses array reference or just copy array values or not)
		ballBuffers.accelerationBuffer.SetData( ballAccelerations );
		ballBuffers.velocityBuffer.SetData( ballVelocities );
		ballBuffers.positionBuffer.SetData( ballPositions );
		ballBuffers.resultBuffer.SetData( ballResult );
		ballBuffers.massBuffer.SetData( ballMasses );


		/* Sets the buffers of the shader function. I don't know why it wants function index (kernel i mean) from us but
		/  i heard that it has a max buffer limit. 8 or maybe 10 per function, depends on the platform. Maybe its related to  
		/ kernel and functions. I know very little about how gpgpu works. Research if you want to feel comfortable*/
		physicsShader.SetBuffer( mainKernel, "accelerationBuff", ballBuffers.accelerationBuffer );
		physicsShader.SetBuffer( mainKernel, "velocityBuff", ballBuffers.velocityBuffer );
		physicsShader.SetBuffer( mainKernel, "positionBuff", ballBuffers.positionBuffer );
		physicsShader.SetBuffer( mainKernel, "resultBuff", ballBuffers.resultBuffer );
		physicsShader.SetBuffer( mainKernel, "massBuff", ballBuffers.massBuffer );

		physicsShader.SetBuffer( setPosKernel, "accelerationBuff", ballBuffers.accelerationBuffer );
		physicsShader.SetBuffer( setPosKernel, "velocityBuff", ballBuffers.velocityBuffer );
		physicsShader.SetBuffer( setPosKernel, "positionBuff", ballBuffers.positionBuffer );
		physicsShader.SetBuffer( setPosKernel, "resultBuff", ballBuffers.resultBuffer );
		physicsShader.SetBuffer( setPosKernel, "massBuff", ballBuffers.massBuffer );


		UpdateShaderValues(); // We update shader values constantly so we can change and adjust values from Inspector.

		/* Dispatch function of the shader is basically execute command for the shader. 
		/ I don't know about the parameters too much, but i guess kernel is the index of
		/ function that we want to execute and the ball count is thread count i guess?
		/ I might be wrong.*/
		physicsShader.Dispatch( mainKernel, ballCount, 1, 1 );

		ballBuffers.resultBuffer.GetData( ballResult ); // Gets the data from GPU to our ballResult array.
	}


	// Update is called once per frame
	void FixedUpdate()
	{

		UpdateShaderValues();
		RunComputeShader();


	}



	void UpdateShaderValues()
	{
		// Simply sets shader global values.
		physicsShader.SetFloat( "alphaMass", alphaMass );
		physicsShader.SetFloat( "friction", frictionRate );
		physicsShader.SetFloat( "borderForceMagnitude", borderForce );
		physicsShader.SetFloat( "gravity", gravity );  // Strangely primitive data types doesnt need kernel. I don't know why.
		physicsShader.SetVector( "alphaBall", alphaObject.position );
		physicsShader.SetVector( "xPosRange", xRange );
		physicsShader.SetVector( "yPosRange", yRange );
		physicsShader.SetVector( "zPosRange", zRange );

	}


	void RunComputeShader()
	{
	
		physicsShader.Dispatch( mainKernel, ballCount, 1, 1 ); // Execute command for our shader.

		for( int i = 0 ; i < ballCount ; i++ )
		{
			ballObjects[ i ].transform.position = ballResult[ i ]; // Simple updates each position of the our gameobject array. 
		}
		physicsShader.Dispatch( setPosKernel, ballCount, 1, 1 );
		ballBuffers.resultBuffer.GetData( ballResult );
		

		 // Gets the data from GPU to our ballResult array.

		/* Important Note: Transforming information from cpu to gpu or gpu to cpu is an expensive 
		 * operation, and the funny thing is we don't even send the new imformation, bu just pass it
		 * from a buffer in gpu to cpu and pass it from cpu to another buffer in gpu. We can do this
		 * exact same operation inside the shader but (UPDATE - It works now)  i tried and failed. 
		 * maybe later i can implement this but for now, it will be work as it is. */
		

		/* Another important note: If you execute(dispatch) the function in shader and 
		 * instantly request data from that shader, you have to wait for the execution
		 * to end. But if you execute (dispatch) the function in shader and do your own 
		 * stuff (Like updating positions of the gameobject, anything not related to 
		 * compute shader) you wait far more less. I did this by executing compute shader
		 * once in the start function, get the data from it, execute the same function again
		 * but in update()/runComputeShader() function, while the shader function is still
		 * executing, i update the gamebjects in the world from the information i got from
		 * the last shader function execution. This means, my screen renders the 1 frame 
		 * behind from the shader values, but who cares if we render 1 frame behind in exchange 
		 * of more framerate. It's a common technique, even our computer does that. Everything
		 * we see in the screen comes 1 frame behind. So its okey. */


	}

}
