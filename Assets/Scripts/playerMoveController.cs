using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace moveController
{


    public class playerMoveController : MonoBehaviour
    {
        [Header("重力")]
        public bool isEnableGravity;
        private Vector2 GravityCurrentSpeed;
        [Range(0f,1f)]
        public float PlayerGravity;
        [Range(0f, 1f)]
        public float PlayerGravitySpeedMax;
        [Header("碰撞体")]
        public int playerMask=8;
        private BoxCollider2D boxCollider;
        private Vector3 colliderSize,colliderPosition;
        private Collider2D[] colliders;
        public bool isOnGround;
        [Header("冲刺")]
        [Tooltip("最大冲刺次数")]
        public int dashCountMax;
        private int dashCount;
        private Vector2 dashDirection;//之后改为private
        private Vector2 DashCurrentSpeed;
        private Vector2 playerCurrentSpeed;
        private dashState dashStateNow;
        private int dashNowFrame;
        [Header("冲刺过程运动调整")]
        [Header("冲刺加速")]
        [Tooltip("冲刺加速的帧率")]
        [SaveDuringPlay]
        public int dashSpeedUpFrame;//冲刺加速的帧率
        [Tooltip("每帧加速的速度")]
        [SaveDuringPlay]
        public float dashSpeed;//每帧加速的速度
        [Header("速度保持状态")]
        [SaveDuringPlay]
        public int dashSpeedStayFrame;
        [Header("空气阻力")]
        [SaveDuringPlay]
        public int dashSpeedDecayFrame;
        [SaveDuringPlay]
        public float dashSpeedDecay;
        [Header("是否防止卡墙里")]
        public bool isEnableAvoidStuck;
        public Collider2D[] avoidStuckCollider;
        private Vector3 lastPos;//用于防止卡墙里
        private CapsuleCollider2D capsuleColliderForAvoidStuck;
        [Range(0f,1f)]
        public float avoidSize;
        private Rigidbody2D rb2d; 
        enum dashState
        {
            noDash=0,
            speedUp=1,
            staySpeed=2,
            speedDecay=3
        }
        private void Awake()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }
            rb2d = GetComponent<Rigidbody2D>();
            capsuleColliderForAvoidStuck = GetComponent<CapsuleCollider2D>();
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            OnGroundCheck();
            ResetDashCountOnGround();
        }
        private void FixedUpdate()
        {
            OnGroundCheck();
            HandleGravity();
            DashMoveHandle();
            MoveHandle();
            AvoidStuck();

#if UNITY_EDITOR

#endif
        }


        #region 重力
        //处理重力
        private void HandleGravity()
        {
            if (isEnableGravity && !isOnGround)
            {
                float _speed = -Mathf.Clamp(-GravityCurrentSpeed.y + PlayerGravity, 0, PlayerGravitySpeedMax);
                GravityCurrentSpeed = new Vector2(0, _speed);
            }
            else 
            {
                GravityCurrentSpeed = new Vector2(0, 0);
            }
        }
        #endregion
        #region 冲刺处理
        //重点:冲刺
        [EditorButton]
        public void Dash(Vector2 _dashDireciton)
        {
            if (--dashCount >= 0)
            {
                dashDirection = _dashDireciton;
                dashNowFrame = 0;
                dashStateNow = dashState.speedUp;
                DashCurrentSpeed = Vector2.zero;
            }
        }
        private void DashMoveHandle()
        {
            switch (dashStateNow)
            {
                case dashState.noDash:DashSpeedStop(); break;
                case dashState.speedUp: DashSpeedup(); break;
                case dashState.staySpeed: DashSpeedStay(); break;
                case dashState.speedDecay: DashSpeedDecay(); break;
            }
        }
        private void DashSpeedup()
        {
            DashCurrentSpeed += dashDirection.normalized * dashSpeed;
            if(++dashNowFrame>dashSpeedUpFrame)
            {
                dashStateNow = dashState.staySpeed;
                dashNowFrame = 0;
            }
        }

        private void DashSpeedStay()
        {
            if (++dashNowFrame > dashSpeedStayFrame)
            {
                dashStateNow = dashState.speedDecay;
                dashNowFrame = 0;
            }
        }
        private void DashSpeedDecay()//空气阻力 施加运动方向相反的力
        {
            DashCurrentSpeed -= DashCurrentSpeed.normalized * dashSpeedDecay;
            if (++dashNowFrame > dashSpeedDecayFrame)
            {
                dashStateNow = dashState.noDash;
                dashNowFrame = 0;
            }
        }
        private void DashSpeedStop()
        {
            DashCurrentSpeed=new Vector2(0,0);
        }
        #endregion
        #region 移动处理
        //处理速度
        private void MoveHandle()
        {
            //Debug.Log(playerCurrentSpeed);
            playerCurrentSpeed=DashCurrentSpeed+GravityCurrentSpeed;
            transform.Translate(playerCurrentSpeed);
            //rb2d.velocity = playerCurrentSpeed;
            Vector2 pos=transform.position;
            Debug.DrawLine(transform.position,pos+DashCurrentSpeed*10,Color.red);
        }
        #endregion
        #region 冲刺次数相关
        private bool ResetDashCountOnGround()
        {
            if(isOnGround && dashStateNow!=dashState.speedUp)
            {
                ResetDashCount();
                return true;
            }    
            return false;
        }
        public int ResetDashCount()
        {
            dashCount = dashCountMax;
            return dashCount;
        }
        #endregion
        #region 地面检测
        bool OnGroundCheck()
        {
                Vector2 pos = transform.position;
                colliderPosition = boxCollider.offset + pos;
                colliderSize = boxCollider.size * transform.localScale;            
                LayerMask ignoreMask = ~(1 << playerMask);
                colliders = Physics2D.OverlapBoxAll(colliderPosition, colliderSize, 0, ignoreMask);
                if (colliders.Length != 0)
                {
                    isOnGround = true;
                    return true;
                }
                else
                {
                    isOnGround = false;
                    return false;
                }
        }
        private void AvoidStuck() //如果检测到卡墙里，则返回上一个没卡墙里的位置
        {
            if (isEnableAvoidStuck)
            {
                LayerMask ignoreMask = ~(1 << playerMask);
                Vector2 pos = transform.position;
                pos = capsuleColliderForAvoidStuck.offset + pos;
                Vector2 size = capsuleColliderForAvoidStuck.size * avoidSize * transform.localScale;
                avoidStuckCollider = Physics2D.OverlapCapsuleAll(pos, size, 0, 0, ignoreMask);
                if (avoidStuckCollider.Length == 0)//没卡墙里
                {
                    lastPos = transform.position;
                }
                else
                {
                    transform.position = lastPos;
                    //transform.TransformPoint(lastPos);
                    Debug.Log("卡墙里 返回");
                }
            }
        }
        #endregion
    }
}
