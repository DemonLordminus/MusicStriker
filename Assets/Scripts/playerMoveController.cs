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
        private CapsuleCollider2D capsuleCollider;
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
        enum dashState
        {
            noDash=0,
            speedUp=1,
            staySpeed=2,
            speedDecay=3
        }
        private void Awake()
        {
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
            if (--dashCount > 0)
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

            Vector2 pos=transform.position;
            Debug.DrawLine(transform.position,pos+DashCurrentSpeed*10,Color.red);
        }
        #endregion
        #region ��̴������
        private bool ResetDashCountOnGround()
        {
            if(isOnGround)
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
            Vector2 pos= transform.position;
            colliderPosition=capsuleCollider.offset+pos;
            colliderSize=capsuleCollider.size*transform.localScale;
            //Debug.DrawLine(colliderPosition,)
            LayerMask ignoreMask = ~(1 << playerMask);
            colliders = Physics2D.OverlapCapsuleAll(colliderPosition,colliderSize,0,0,ignoreMask);
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
        private void leavelGround()
        {

        }
        #endregion
    }
}
