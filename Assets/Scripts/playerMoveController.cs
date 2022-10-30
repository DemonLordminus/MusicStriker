using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

namespace moveController
{


    public class playerMoveController : MonoBehaviour
    {
        [Header("����")]
        public bool isEnableGravity;
        private Vector2 GravityCurrentSpeed;
        [Range(0f,1f)]
        public float PlayerGravity;
        [Range(0f, 1f)]
        public float PlayerGravitySpeedMax;
        [Header("��ײ��")]
        public int playerMask=8;
        public BoxCollider2D boxCollider;
        private Vector3 colliderSize,colliderPosition;
        private Collider2D[] colliders;
        public bool isOnGround;
        [Header("���")]
        [Tooltip("����̴���")]
        public int dashCountMax;
        private int dashCount;
        private Vector2 dashDirection;//֮���Ϊprivate
        private Vector2 DashCurrentSpeed;
        private Vector2 playerCurrentSpeed;
        private dashState dashStateNow;
        private int dashNowFrame;
        [Header("��̹����˶�����")]
        [Header("��̼���")]
        [Tooltip("��̼��ٵ�֡��")]
        [SaveDuringPlay]
        public int dashSpeedUpFrame;//��̼��ٵ�֡��
        [Tooltip("ÿ֡���ٵ��ٶ�")]
        [SaveDuringPlay]
        public float dashSpeed;//ÿ֡���ٵ��ٶ�
        [Header("�ٶȱ���״̬")]
        [SaveDuringPlay]
        public int dashSpeedStayFrame;
        [Header("��������")]
        [SaveDuringPlay]
        public int dashSpeedDecayFrame;
        [SaveDuringPlay]
        public float dashSpeedDecay;
        [Header("�Ƿ��ֹ��ǽ��")]
        public bool isEnableAvoidStuck;
        public Collider2D[] avoidStuckCollider;
        private Vector3 lastPos;//���ڷ�ֹ��ǽ��
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


        #region ����
        //��������
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
        #region ��̴���
        //�ص�:���
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
        private void DashSpeedDecay()//�������� ʩ���˶������෴����
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
        #region �ƶ�����
        //�����ٶ�
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
        #region ��̴������
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
        #region ������
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
        private void AvoidStuck() //�����⵽��ǽ��򷵻���һ��û��ǽ���λ��
        {
            if (isEnableAvoidStuck)
            {
                LayerMask ignoreMask = ~(1 << playerMask);
               
                avoidStuckCollider = Physics2D.OverlapBoxAll(colliderPosition, colliderSize * avoidSize, 0, 0, ignoreMask);
                if (avoidStuckCollider.Length == 0)//û��ǽ��
                {
                    lastPos = transform.position;
                }
                else
                {
                    //transform.position = lastPos;
                    transform.TransformPoint(lastPos);
                    
                }
            }
        }
        #endregion
    }
}
