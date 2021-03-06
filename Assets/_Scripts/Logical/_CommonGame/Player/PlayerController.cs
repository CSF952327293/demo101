﻿using UnityEngine;
using System.Collections;
using System.Text;
using SuperHero.Entity;
namespace SuperHero.Logical
{
	/// <summary>
	/// 主要控制的是人物的位置信息
	/// </summary>
	public class PlayerController : MonoBehaviour 
	{
		#region 变量的定义
		/// <summary>
		/// 轨道之间的距离，到时候要和模型相匹配
		/// </summary>
		public float xTrackOffset=5f;
		/// <summary>
		/// 更换轨道的时间
		/// </summary>
		public float xChangTime=0.5f;
		/// <summary>
		/// 重力大小，下落时为双倍重力
		/// </summary>
		public float gravity=9.8f;
		
		public float jumpSpeed=10f;
		/// <summary>
		/// 前进的速度
		/// </summary>
		public float moveSpeed=10f;
		
		
		/// <summary>
		/// 前进方向的方向向量.注：需要单位向量化
		/// </summary>
		public Vector3 mDirection=Vector3.zero;
		/// <summary>
		/// 相对于路标点的位移
		/// </summary>
		public Vector3 mRoadOffset=Vector3.zero;
		
		public eRunMode mRunMode=eRunMode.straight;
		
		private CurrentGameInfo currentGameInfo=new CurrentGameInfo ();
		
		public CurrentGameInfo CurrentGameInfo {
			get 
			{
				return currentGameInfo;
			}
			set 
			{
				currentGameInfo = value;
			}
		}
		
		public CatapultController CurrentCatapultCtrl{get;set;}
		
		private PlayerAnimationController currentAntorCtrl;
		
		public PlayerAnimationController CurrentAntorCtrl {
			get {
				return currentAntorCtrl;
			}
			set {
				currentAntorCtrl = value;
			}
		}
		
		private ClimbController currentClimbCtrl;
		
		public ClimbController CurrentClimbCtrl {
			get {
				return currentClimbCtrl;
			}
			set {
				currentClimbCtrl = value;
			}
		}
		
		
		private FlyController currentFlyCtrl;
		
		public FlyController CurrentFlyCtrl {
			get {
				return currentFlyCtrl;
			}
			set {
				currentFlyCtrl = value;
			}
		}		
		
		private PropertyMaster currentPM;
		
		public PropertyMaster CurrentPM {
			get {
				return currentPM;
			}
			set {
				currentPM = value;
			}
		}
		
		public Vector3 fallCenter;
		public float fallHeight;
		/// <summary>
		/// 当前角色身上的角色控制器
		/// </summary>
		public CharacterController mCC;
		/// <summary>
		/// 当前场景的输入控制器
		/// </summary>
		[HideInInspector]
		public InputControl mIC;
		/// <summary>
		/// 标记:是否处在切换轨道的过程中
		/// </summary>
		[HideInInspector]
		private bool bIsChangeTrack=false;
		/// <summary>
		/// 本地相坐标的X偏移量，注：进度不包含Y轴
		/// </summary>
		private float localXOffset=0f;
		/// <summary>
		/// 跳跃的状态：木有跳，一级跳，二级跳
		/// </summary>
		public eJumpState mJumpState=eJumpState.NoneJump;
		/// <summary>
		/// 下落时是否加速下降
		/// </summary>
		public eFallDown mFallDown=eFallDown.normal;
		/// <summary>
		/// 当前运行在哪个轨道
		/// </summary>
		public eTrack mTrack=eTrack.middle;
		/// <summary>
		/// 轨道的数量3or5
		/// </summary>
		public eTrackNum mTrackNum=eTrackNum.Three;
		/// <summary>
		/// Y轴方向的速度
		/// </summary>
		private float mYSpeed=0f;
		/// <summary>
		/// 是否处在停留在空中的状态
		/// </summary>
		private bool mIsHover=false;
		/// <summary>
		/// The is pause.
		/// </summary>
		private bool isPause=false;


		private bool isDead=false;

		public bool IsDead {
			get {
				return isDead;
			}
			set {
				isDead=value;
			}
		}		
		
		Vector3 [] mCC_Center=new Vector3[2];
		float [] mCC_Height=new float[2];
		/// <summary>
		/// 攻击到达的步骤
		/// </summary>
		eAttackStage mAttackStage=eAttackStage.None;
		public float attackGap=1f;

		public VoidDelegate onDead;
		public VoidDelegate onRevival;
		#endregion
		#region interface
		
		void Awake()
		{
			GlobalInGame.currentPC=this;
		}
		// Use this for initialization
		void Start () {
			
			currentGameInfo.HP_Max=100;
			currentGameInfo.MP_Max=100;
			currentGameInfo.HP=100;
			currentGameInfo.MP=100;
			
			currentClimbCtrl=GetComponent<ClimbController>();
			currentFlyCtrl=GetComponent<FlyController>();
			currentAntorCtrl=GetComponent<PlayerAnimationController>();
			CurrentCatapultCtrl=GetComponent<CatapultController>();
			currentPM=GetComponent<PropertyMaster>();
			
			mCC=GetComponent<CharacterController>();
			mIC=GameObject.Find("InputController").GetComponent<InputControl>();
			
			if(mCC!=null&&mIC!=null)//注册委托
			{
				RegisterOP();
				
			}
			mYSpeed=gravity;
			mCC_Center[0]=mCC.center;
			mCC_Height[0]=mCC.height;
			mCC_Height[1]=fallHeight;
			mCC_Center[1]=fallCenter;
			
			if(currentAntorCtrl)
			{
				if(currentAntorCtrl.Amtor==null)
					currentAntorCtrl.Amtor=GetComponent<Animator>();
				currentAntorCtrl.RunA();
			}
		}
		
		void OnEnable()
		{
			//Start();
		}
		
		// Update is called once per frame
		void Update () {
			if(!isPause)
			{
				UpdateRotate();
				UpdateOffest();
				UpdateHeight();
				
				UpdatePositon();
			}
			if(Input.GetKeyDown(KeyCode.A))
			   {
				Attack();
			}
		}
		
		//		void OnGUI()
		//		{
		//			GUILayout.Label(mJumpState.ToString());
		//			GUILayout.Label(mYSpeed.ToString());
		//		}
		
		
		
		/// <summary>
		/// 受到伤害时调用
		/// </summary>
		/// <param name="blood">Blood.</param>
		/// <param name="hit">Hit.</param>
		/// <param name="isDestroy">是否摧毁障碍物</param>
		public void GetHurt(int blood,ControllerColliderHit hit,bool isDestroy)
		{
			if(isDestroy)
				Destroy(hit.collider.gameObject);
			GetHurt(blood);
			//			//在直行道上面行走
			//			if(mRunMode==eRunMode.straight)
			//			{
			//				if(bIsChangeTrack==true)
			//				{
			//					Debuger.Log("换轨中接触到障碍物:"+hit.collider.name+"  HP减少:"+blood.ToString());
			//					mSurplusChangeTime=xChangTime-mSurplusChangeTime;
			//					if(mEndPos-mStartPos>0f)//Right
			//					{
			//						mTrack--;
			//					}
			//					else//Left
			//					{
			//						mTrack++;
			//					}
			//					float temp=mEndPos;
			//					mEndPos=mStartPos;
			//					mStartPos=temp;
			//					bIsChangeTrack=true;
			//					
			//					currentGameInfo.HP-=blood;
			//				}
			//				else
			//				{
			//					Debuger.Log("正面接触到障碍物:"+hit.collider.name+"  HP减少:"+blood.ToString());
			//					currentGameInfo.HP-=blood;
			//				}
			//				Debuger.Log("Destoryed "+hit.collider.name);
			//				Destroy( hit.collider.gameObject);
			//				
			//				if(currentGameInfo.HP<=0f)
			//				{
			//					Debuger.Log("Dead!!!!!");
			//					currentGameInfo.HP=0f;
			//				}
			//				
			//				
			//			}
		}
		
//		public void GetGoldCoin()
//		{
//			currentGameInfo.goldCions++;
//			currentPM.GoldIN();
//		}
//		
		public void GetProps(PropInfo prop)
		{
			currentPM.GetProp(prop);
		}
		
		public void GetHurt(int blood)
		{
			//在直行道上面行走
			if(mRunMode==eRunMode.straight)
			{
				if(bIsChangeTrack==true)
				{
					mSurplusChangeTime=xChangTime-mSurplusChangeTime;
					if(mEndPos-mStartPos>0f)//Right
					{
						mTrack--;
					}
					else//Left
					{
						mTrack++;
					}
					float temp=mEndPos;
					mEndPos=mStartPos;
					mStartPos=temp;
					bIsChangeTrack=true;
					
					currentGameInfo.HP-=blood;
				}
				else
				{
					currentGameInfo.HP-=blood;
				}
				
				if(currentGameInfo.HP<=0f)
				{
					Debuger.Log("Dead!!!!!");
					Dead();
					currentGameInfo.HP=0;
				}
				
				
			}
		}
		
		
		#endregion
		#region OperationReflect
		[HideInInspector]
		private float mStartPos=0f;
		[HideInInspector]
		private float mEndPos=0f;
		[HideInInspector]
		private float mSurplusChangeTime=0f;
		void TurnLeft()
		{
			if(mRunMode!=eRunMode.straight)
				return;
			if(mTrackNum==eTrackNum.Three)
			{
				if(mTrack!=eTrack.midLeft&&!bIsChangeTrack)
				{
					mStartPos=((int)mTrack-3)*xTrackOffset;
					mEndPos=((int)mTrack-4)*xTrackOffset;
					mSurplusChangeTime=xChangTime;
					mTrack--;
					bIsChangeTrack=true;
					if(currentAntorCtrl!=null)
						currentAntorCtrl.TurnLeft();
				}
			}
		}
		
		void TurnRight()
		{
			
			if(mTrackNum==eTrackNum.Three)
			{
				if(mTrack!=eTrack.midRight&&!bIsChangeTrack)
				{
					mStartPos=((int)mTrack-3)*xTrackOffset;
					mEndPos=((int)mTrack-2)*xTrackOffset;
					mSurplusChangeTime=xChangTime;
					mTrack++;
					bIsChangeTrack=true;
					if(currentAntorCtrl!=null)
						currentAntorCtrl.TurnRight();
				}
			}
		}
		
		
		public void Jump(float ySpeed)
		{
			if( mJumpState==eJumpState.NoneJump)
			{
				mYSpeed=-ySpeed;
				mJumpState=eJumpState.FirstJump;
				currentAntorCtrl.JumpA();
			}
			else if(mJumpState==eJumpState.FirstJump)
			{
				mYSpeed-=ySpeed;
				mJumpState=eJumpState.DoubleJump;
				currentAntorCtrl.JumpTwo();
			}
		}
		
		public void Jump()
		{
			Jump(jumpSpeed);
		}
		
		
		void Attack()
		{
			if(mRunMode==eRunMode.straight)
			{
				if(mJumpState==eJumpState.NoneJump)
				{
					switch(mAttackStage)
					{
					case eAttackStage.None:
						currentAntorCtrl.Attack();
						mAttackStage=eAttackStage.First;
						StartCoroutine(AttackFirst());
						break;
					case eAttackStage.First:
						currentAntorCtrl.AttackTwo();
						mAttackStage=eAttackStage.Double;
						StopCoroutine(AttackFirst());
						StartCoroutine(AttackDouble());
						break;
					case eAttackStage.Double:
						break;
					default:
						break;
					}
				}
				else
				{
					currentAntorCtrl.FlyAttack();
				}
			}
			else if(mRunMode==eRunMode.fly)
			{
				currentAntorCtrl.FlyAttack();
			}

			RaycastHit hit;
			Debug.DrawLine(transform.position+new Vector3(0f,1f,0f),transform.position+new Vector3(0f,0f,5f));

			if(Physics.Raycast(transform.position+new Vector3(0f,1f,0f),transform.forward,out hit,5f,1<<11))
			{
				//Destroy(hit.collider.gameObject);
				Monster mm=hit.collider.GetComponent<Monster>();
				Debug.Log("hit");
				mm.BeAttack();
//				Debug.Log(hit.collider.gameObject.layer.ToString());
			}

			
		}
		
		IEnumerator AttackFirst()
		{
			yield return new WaitForSeconds(attackGap);
			if(mAttackStage==eAttackStage.First)
				mAttackStage=eAttackStage.None;
			yield return null;
		}
		IEnumerator AttackDouble()
		{
			yield return new WaitForSeconds(attackGap);
			if(mAttackStage==eAttackStage.Double)
				mAttackStage=eAttackStage.None;
			yield return null;
		}
		/// <summary>
		/// 下降
		/// </summary>
		void FallDown()
		{
			if(currentAntorCtrl)
			{
				RaycastHit hit;
				if(Physics.Raycast(transform.position,Vector3.down,out hit,mCC.height*0.5f, 1<<9))
				{
//					Debug.Log("height: "+hit.distance.ToString());
					StartCoroutine(mCCFallDown());
					currentAntorCtrl.Shovel();
				}
				else if(mJumpState!= eJumpState.NoneJump)
				{
					mFallDown= eFallDown.doubleGravity;
				}
			}
		}
		
		IEnumerator mCCFallDown()
		{
			mCC.height=mCC_Height[1];
			mCC.center= mCC_Center[1];
			yield return new WaitForSeconds(0.9f);
			mCC.height=mCC_Height[0];
			mCC.center= mCC_Center[0];
			yield return null;
		}


		public void Dead()
		{
			isDead=true;
			mIsHover=true;
			isPause=true;

			switch(mRunMode)
			{
			case eRunMode.straight:
				isDead=true;
				mIsHover=true;
				isPause=true;
				currentAntorCtrl.Dead();
				break;

			case eRunMode.climb:
				CurrentClimbCtrl.Dead();
				break;
			default:
				break;
			}

		}
		/// <summary>
		/// 复活
		/// </summary>
		public void Revival()
		{

			switch(mRunMode)
			{
			case eRunMode.straight:
				mIsHover=false;
				isPause=false;
				isDead=false;
				currentAntorCtrl.RunA();
				bool isOK=false;
				int index=1;
				while(isOK==false)
				{
					RaycastHit hit;
					Vector3 start=new Vector3(transform.position.x,transform.position.y+5f,transform.position.z+5f*index);
					Vector3 direction=Vector3.down;
					Debug.DrawLine(start,start+new Vector3(0f,-10f,0f));
					if(Physics.Raycast(start,direction,out hit,10f,1<<9))
					{
						transform.position=hit.point;
						isOK=true;
					}
					index++;
				}
				break;
				
			default:
				break;
			}

		}


		#endregion
		#region Method
		float hoverTime=1f;
		/// <summary>
		/// 停留空中一定的时间
		/// </summary>
		/// <param name="time">停留的时间</param>
		void Hover(float time)
		{
			hoverTime=time;
			StartCoroutine(iHover());
		}
		/// <summary>
		/// 无限期滞留
		/// </summary>
		void Hover()
		{
			mIsHover=true;
		}
		/// <summary>
		/// 取消滞留空中，开始降落
		/// </summary>
		void Land()
		{
			mIsHover=false;
		}
		/// <summary>
		/// 内部方法，延时的协同实现
		/// </summary>
		IEnumerator iHover()
		{
			mIsHover=true;
			yield return new WaitForSeconds(hoverTime);
			mIsHover=false;
			yield return null;
		}
		
		
		
		public void ReStart(Vector3 startPosition,Vector3 startDirection)
		{
			mIsHover=false;
			isPause=false;
			mRunMode=eRunMode.straight;
			transform.eulerAngles=startDirection;
			mDirection=startDirection;
			float startXPosition=0f;
			switch(mTrack)
			{
			case eTrack.left:
				startXPosition=-2f*xTrackOffset;
				break;
			case eTrack.midLeft:
				startXPosition=-xTrackOffset;
				break;
			case eTrack.middle:
				startXPosition=0f;
				break;
			case eTrack.midRight:
				startXPosition=xTrackOffset;
				break;
			case eTrack.right:
				startXPosition=2f*xTrackOffset;
				break;
			default:break;
			}
			Vector3 currentP=transform.right*startXPosition+startPosition;
			transform.position=currentP;
			mSurplusChangeTime=0f;
		}
		
		
		
		public void Pause()
		{
			isPause=true;
			if(mIC)
			{
				mIC.TurnLeft-=TurnLeft;
				mIC.TurnRight-=TurnRight;
				mIC.JumpOP-=Jump;
				mIC.FallDownOP-=FallDown;
				mIC.Attack-=Attack;
			}
		}
		
		public void Resume()
		{
			isPause=false;
			RegisterOP();
		}
		
		public void Stop()
		{
			isPause=true;
			CancelOP();
			
		}
		
		public void RoundRight(Vector3 center)
		{
			roundCenter=center;
			radius=Vector3.Distance(center,transform.position);
			mRunMode=eRunMode.roundRight;
		}
		public void RoundLeft(Vector3 center)
		{
			roundCenter=center;
			radius=Vector3.Distance(center,transform.position);
			mRunMode=eRunMode.roundLeft;
		}
		/// <summary>
		/// Ready to Climb
		/// </summary>
		public void ClimbStart(float ySpeed=20f)
		{
			
			CancelOP();
			Jump(ySpeed);
			
		}
		
		public void Climbing(Vector3 climbDirection)
		{
			localYOffset=0f;
			mCC.enabled=true;
			mIsHover=true;
			isPause=true;
			mRunMode=eRunMode.climb;
			currentClimbCtrl.ClimbStart(this);
		}
		
		public void ClimbEnd(Vector3 roadDirection)
		{
			currentClimbCtrl.EndClimb(roadDirection);
			mCC.enabled=true;
			mIsHover=false;
			isPause=false;
			RegisterOP();
			mRunMode=eRunMode.straight;
			mJumpState=eJumpState.NoneJump;
		}
		
		public void FlyStart(float ySpeed,float speed,float gravity)
		{
			mRunMode=eRunMode.fly;
			mIsHover=true;
			isPause=true;
			CancelOP();
			currentFlyCtrl.StartFly(this,ySpeed,speed,gravity);
			
		}
		
		public void ContinueFly(float ySpeed=10f)
		{
			currentFlyCtrl.ContinueFly(ySpeed);
		}
		
		public void CatapultReady()
		{
			if(CurrentCatapultCtrl==null)
			{
				Debuger.Log("CatapultCtrl is null!!!");
				return;
			}
			CurrentCatapultCtrl.CatapultReady();
		}
		
		public void Catapulting(Vector3 start,Vector3 target,float gravity,float ySpeed)
		{
			CurrentCatapultCtrl.Catapulting(start,target,gravity,ySpeed);
		}
		
		public void CatapultEnd()
		{
			CurrentCatapultCtrl.CatapultEnd();
		}
		
		public void RegisterOP()
		{
			if(mCC!=null&&mIC!=null)//注册委托
			{
				if(mIC.TurnLeft==null)
				{
					mIC.TurnLeft+=TurnLeft;
					Debug.Log("RegisterOP");
				}
				if(mIC.TurnRight==null)
					mIC.TurnRight+=TurnRight;
				if(mIC.JumpOP==null)
					mIC.JumpOP+=Jump;
				if(mIC.FallDownOP==null)
					mIC.FallDownOP+=FallDown;
				if(mIC.Attack==null)
					mIC.Attack+=Attack;
			}
		}
		public void CancelOP()
		{
			if(mIC)
			{
				Debug.Log("CancelOP");
				mIC.TurnLeft-=TurnLeft;
				mIC.TurnRight-=TurnRight;
				mIC.JumpOP-=Jump;
				mIC.FallDownOP-=FallDown;
				mIC.Attack-=Attack;
			}
		}
		
		
		
		#endregion
		#region UpdatePositionAndRotation
		
		/// <summary>
		/// 应用偏移量:Tps:换轨过程中碰撞不可碰撞，不然会偏移错误，可以根据初始位置和方向进行矫正，后期修改
		/// </summary>
		void UpdateOffest()
		{
			if(bIsChangeTrack)
			{
				if(mRunMode==eRunMode.straight)
				{
					mSurplusChangeTime-=Time.deltaTime;
					if(mSurplusChangeTime<0f)
					{
						localXOffset=((mSurplusChangeTime+Time.deltaTime)/xChangTime)*(mEndPos-mStartPos);
						bIsChangeTrack=false;
						mSurplusChangeTime=0f;
					}
					else if(mSurplusChangeTime==0f)
					{
						localXOffset=0f;
						bIsChangeTrack=false;
						mSurplusChangeTime=0f;
					}
					else
					{
						localXOffset=(Time.deltaTime/xChangTime)*(mEndPos-mStartPos);
						
					}
				}
			}
			else
			{
				localXOffset=0f;
			}
			
		}
		float localYOffset=0f;
		
		/// <summary>
		/// 更新高度
		/// </summary>
		void UpdateHeight()
		{
			if(!mIsHover)
			{
				if(mYSpeed<gravity)
				{
					//加速下落的实现a*2,normal*1
					if(mFallDown==eFallDown.normal)
						mYSpeed+=gravity*Time.deltaTime;
					else if(mFallDown==eFallDown.doubleGravity)
						mYSpeed+=gravity*Time.deltaTime*5f;
					//mYSpeed+=gravity*Time.deltaTime*2f;
				}
				else if(mYSpeed>gravity)
				{
					mYSpeed=gravity;
				}
				if(mCC)
				{
					localYOffset=-mYSpeed*Time.deltaTime;
					if(mJumpState==eJumpState.FirstJump||mJumpState==eJumpState.DoubleJump)
					{
						RaycastHit hit;
						if(Physics.Raycast(transform.position,Vector3.down,out hit,10f))
						{
							Debug.DrawLine(transform.position,hit.point,Color.red);
							if(hit.distance<=0.1f&&mYSpeed>0f)
							{
								mJumpState=eJumpState.NoneJump;
								//落地后清除加速下降状态
								mFallDown=eFallDown.normal;
							}
						}
						
					}
					
				}
			}
			else//滞留空中
			{
				localYOffset=0f;
			}
		}
		
		/// <summary>
		/// 做圆周运动的时候的圆心坐标
		/// </summary>
		private Vector3 roundCenter=Vector3.zero;
		/// <summary>
		/// 做圆周运动的时候的半径
		/// </summary>
		private float radius=1f;
		/// <summary>
		/// 应用方向，direction的Y轴信息
		/// </summary>
		void UpdateRotate()
		{
			switch(mRunMode)
			{
			case eRunMode.straight:
				transform.eulerAngles=mDirection;
				break;
			case eRunMode.roundRight:
				transform.rotation=Quaternion.LookRotation(roundCenter-transform.position);
				transform.Rotate(new Vector3 (0f,-90f,0f),Space.World);
				transform.eulerAngles=new Vector3(0f,transform.eulerAngles.y,0f);
				Debug.DrawLine(roundCenter,transform.position,Color.red);
				break;
			case eRunMode.roundLeft:
				transform.rotation=Quaternion.LookRotation(roundCenter-transform.position);
				transform.Rotate(new Vector3 (0f,90f,0f),Space.World);
				transform.eulerAngles=new Vector3(0f,transform.eulerAngles.y,0f);
				Debug.DrawLine(roundCenter,transform.position,Color.red);
				break;
			case eRunMode.climb:
				break;
			default:break;
			}
		}
		
		void UpdatePositon()
		{
			switch(mRunMode)
			{
			case eRunMode.straight:
				Vector3 moveDirection=new Vector3(localXOffset,localYOffset,moveSpeed*Time.deltaTime);
				moveDirection= transform.TransformDirection(moveDirection);
				mCC.Move(moveDirection);
				break;
			case eRunMode.roundRight:
				float s=moveSpeed*Time.deltaTime;
				float d=s/radius;
				float x=radius-radius*Mathf.Cos(d);
				float h=radius*Mathf.Sin(d);
				mCC.Move(transform.TransformDirection(new Vector3(x,localYOffset,h)));
				break;
			case eRunMode.roundLeft:
				float s1=moveSpeed*Time.deltaTime;
				float d1=s1/radius;
				float x1=radius*Mathf.Cos(d1)-radius;
				float h1=radius*Mathf.Sin(d1);
				mCC.Move(transform.TransformDirection(new Vector3(x1,localYOffset,h1)));
				break;
			case eRunMode.climb:
				//				moveDirection=new Vector3(localXOffset,localYOffset,moveSpeed*Time.deltaTime);
				//				moveDirection= transform.TransformDirection(moveDirection);
				//				transform.position+=moveDirection;
				break;
			default :break;
			}
		}
		
		#endregion
		
		
		#region Enum
		public enum eFallDown
		{
			normal=0,
			doubleGravity=1,
		}
		
		public enum eJumpState
		{
			NoneJump=0,
			FirstJump=1,
			DoubleJump=2,
		};
		
		public enum eTrack
		{
			left=1,
			midLeft=2,
			middle=3,
			midRight=4,
			right=5,
		};
		
		public enum eTrackNum
		{
			Three=3,
			Five=5,
		}
		
		public enum eRunMode
		{
			straight=1,
			roundRight=2,
			roundLeft=3,
			climb=4,
			fly=5,
			changeRoad=6,
		}
		
		public enum eAttackStage
		{
			None=0,
			First=1,
			Double=2,
		}
		
		#endregion
	}
	
}
