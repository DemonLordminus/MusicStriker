using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEditor;

namespace moveController
{


    public class playerMoveController : MonoBehaviour
    {
        [Header("重力")]
        public bool isEnableGravity;
        private Vector2 GravityCurrentSpeed;
        [Range(0f, 1f)]
        public float PlayerGravity;
        [Range(0f, 1f)]
        public float PlayerGravitySpeedMax;
        [Header("碰撞体")]
        public int playerMask = 8;
        private BoxCollider2D boxCollider;
        private Vector3 colliderSize, colliderPosition;
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
        public bool isCloseToEdge;
        public bool isNoSpeedUpWhenTouchEdge;
        //public bool isEnableAvoidStuck;//已停用
        //public Collider2D[] avoidStuckCollider;
        //private Vector3 lastPos;//用于防止卡墙里
        private CapsuleCollider2D capsuleCollider;
        [Range(0f, 1f)]
        public float edgeCheckSize;
        private Rigidbody2D rb2d;
        private Vector2 nextPosOffset;
        public bool isUseMovePostion;
        enum dashState
        {
            noDash = 0,
            speedUp = 1,
            staySpeed = 2,
            speedDecay = 3
        }
        private void Awake()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider2D>();
            }
            rb2d = GetComponent<Rigidbody2D>();
            capsuleCollider = GetComponent<CapsuleCollider2D>();
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
            //AvoidStuck();
          
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
                case dashState.noDash: DashSpeedStop(); break;
                case dashState.speedUp: DashSpeedup(); break;
                case dashState.staySpeed: DashSpeedStay(); break;
                case dashState.speedDecay: DashSpeedDecay(); break;
            }
        }
        private void DashSpeedup()
        {
            DashCurrentSpeed += dashDirection.normalized * dashSpeed;
            if (++dashNowFrame > dashSpeedUpFrame)
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
            DashCurrentSpeed = new Vector2(0, 0);
        }
        #endregion
        #region 移动处理
        //处理速度
        private void MoveHandle()
        {
            //Debug.Log(playerCurrentSpeed);
            playerCurrentSpeed = DashCurrentSpeed + GravityCurrentSpeed;
            CloseToEdge();
            if (isUseMovePostion)
            {
                Vector2 nowPos = transform.position;
                rb2d.MovePosition(playerCurrentSpeed + nowPos);
            }
            else
            {
                transform.Translate(playerCurrentSpeed + nextPosOffset);
                nextPosOffset = Vector2.zero;
            }
            //rb2d.velocity = playerCurrentSpeed;
            Vector2 pos = transform.position;
            Debug.DrawLine(transform.position, pos + DashCurrentSpeed * 10, Color.red);
        }
        #endregion
        #region 冲刺次数相关
        private bool ResetDashCountOnGround()
        {
            if (isOnGround && dashStateNow != dashState.speedUp)
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
        bool OnGroundCheck() //我的代码怎么写的，这里有Bug
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
        //public Transform test1, test2;
        //private void AvoidStuck() //如果检测到卡墙里，则返回上一个没卡墙里的位置
        //{
        //    if (isEnableAvoidStuck)
        //    {
        //        Debug.LogError("该设置已停用，不建议开启");
        //        LayerMask ignoreMask = ~(1 << playerMask);
        //        Vector2 pos = transform.position;
        //        pos = capsuleCollider.offset + pos;
        //        Vector2 size = capsuleCollider.size * avoidSize * transform.localScale;
        //        avoidStuckCollider = Physics2D.OverlapCapsuleAll(pos, size, 0, 0, ignoreMask);
        //        if (avoidStuckCollider.Length == 0)//没卡墙里
        //        {
        //            lastPos = transform.position;
        //        }
        //        else
        //        {
        //            transform.position = lastPos;
        //            #region 废弃代码
        //            //transform.TransformPoint(lastPos);
        //            //Debug.Log("卡墙里 返回");
        //            //Vector3 origin = lastPos;//上一帧的位置
        //            //Vector3 end = transform.position;//触发时的位置
        //            //Vector3 direction = end - origin;//射线方向
        //            ////float distance = Vector3.Distance(origin, end);//射线检测距离
        //            //Vector3 hitpoint;
        //            //transform.position = origin;
        //            //RaycastHit2D hit = Physics2D.Raycast(origin, direction, 30, ignoreMask);//发射射线，只检测与"Target"层的碰撞
        //            //Debug.DrawRay(origin, direction, Color.green, 2);//绘制射线
        //            //Debug.Assert(hit.collider != null, "未检测到起点");
        //            ////if (hit.collider != null)
        //            //{
        //            //    hitpoint = hit.point;//获得该碰撞点
        //            //    test1.position = hitpoint;
        //            //    //direction = origin - hitpoint;
        //            //    ////hitpoint -= direction.normalized;
        //            //    //LayerMask playerLayerMask = (1 << playerMask);
        //            //    //hit = Physics2D.Raycast(hitpoint, direction, 30, playerLayerMask);
        //            //    //Debug.DrawRay(hitpoint, direction, Color.blue, 2);//绘制射线
        //            //    //Vector3 selfHitPoint = hit.point;
        //            //    //test2.position = selfHitPoint;
        //            //    //Vector3 offset = selfHitPoint - origin;
        //            //    Vector3 offset = hitpoint - origin;
        //            //    transform.position = hitpoint;
        //            #endregion

        //        }
        //    }
            
        //}
        private void CloseToEdge()
        {
            if(isCloseToEdge)
            { 
            Vector2 origin = transform.position;
            LayerMask ignoreMask = ~(1 << playerMask);
            Vector2 end = origin + playerCurrentSpeed;
            Vector2 direction = end - origin;
            float distance = direction.magnitude;
            RaycastHit2D hit = Physics2D.CapsuleCast(origin, capsuleCollider.size*edgeCheckSize, capsuleCollider.direction, 0, direction,distance,ignoreMask);
                if (hit.collider != null)
                {
                    Debug.DrawRay(origin, direction, Color.green, 2);//绘制射线
                    //Debug.Log("预计撞击");
                    nextPosOffset = hit.centroid - end;
                    //test1.position = nextPosOffset;
                    //test2.position = nextPosOffset + playerCurrentSpeed;
                    //EditorApplication.isPaused = true;
                    if(isNoSpeedUpWhenTouchEdge)
                    {
                        if(dashStateNow==dashState.speedUp || dashStateNow == dashState.staySpeed)
                        {
                            dashNowFrame = 0;
                            dashStateNow = dashState.speedDecay;
                        }
                    }
                }

            }
        }
        #endregion
    }
}
