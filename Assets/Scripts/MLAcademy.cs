using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class MLAcademy : Agent
{
    private Transform tr;
    private Rigidbody rb;
    public Transform targetTr;
    public Renderer floorRd;        //<== 추가

    //바닥의 색생을 변경하기 위한 머티리얼
    private Material Mt_origin;      //<== 추가
    public Material Mt_fail;          //<== 추가
    public Material Mt_success;         //<== 추가

    //초기화 작업을 위해 한번 호출되는 메소드
    public override void Initialize()
    {
        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody>();
        Mt_origin = floorRd.material;
    }

    //에피소드(학습단위)가 시작할때마다 호출
    public override void OnEpisodeBegin()
    {
        //물리력을 초기화
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //에이젼트의 위치를 불규칙하게 변경
        tr.localPosition = new Vector3(Random.Range(-4.0f, 4.0f)
                                     , 1.0f
                                     , Random.Range(-4.0f, 4.0f));
        targetTr.localPosition = new Vector3(Random.Range(-4.0f, 4.0f)
                                            , 0.5f
                                            , Random.Range(-4.0f, 4.0f));
        StartCoroutine(RevertMaterial());
    }

    IEnumerator RevertMaterial()
    {
        yield return new WaitForSeconds(0.2f);
        floorRd.material = Mt_origin;
    }

    //환경 정보를 관측 및 수집해 정책 결정을 위해 브레인에 전달하는 메소드
    public override void CollectObservations(Unity.MLAgents.Sensors.VectorSensor sensor)
    {
        sensor.AddObservation(targetTr.localPosition);  //3 (x,y,z)
        sensor.AddObservation(tr.localPosition);        //3 (x,y,z)
        sensor.AddObservation(rb.velocity.x);           //1 (x)
        sensor.AddObservation(rb.velocity.z);           //1 (z)
    }

    //브레인(정책)으로 부터 전달 받은 행동을 실행하는 메소드
    public override void OnActionReceived(float[] vectorAction)
    {
        //데이터를 정규화
        float h = Mathf.Clamp(vectorAction[0], -1.0f, 1.0f);
        float v = Mathf.Clamp(vectorAction[1], -1.0f, 1.0f);
        Vector3 dir = (Vector3.forward * v) + (Vector3.right * h);
        Debug.Log($"h={h} v={v}");
        rb.AddForce(dir.normalized * 13.0f);

        //지속적으로 이동을 이끌어내기 위한 마이너스 보상
        SetReward(-0.001f);
    }

    //개발자(사용자)가 직접 명령을 내릴때 호출하는 메소드(주로 테스트용도 또는 모방학습에 사용)
    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Input.GetAxis("Horizontal"); //좌,우 화살표 키 //-1.0 ~ 0.0 ~ 1.0
        actionsOut[1] = Input.GetAxis("Vertical");   //상,하 화살표 키 //연속적인 값
        //Debug.Log($"[0]={actionsOut[0]} [1]={actionsOut[1]}");
    }

    //충돌 감지시 호출하는 메소드
    void OnCollisionEnter(Collision coll)
    {
        if (coll.collider.CompareTag("DEAD_ZONE_SET"))
        {
            floorRd.material = Mt_fail;
            //잘못된 행동일 때 마이너스 보상을 준다.
            SetReward(-1.0f);
            //학습을 종료시키는 메소드
            EndEpisode();
        }

        if (coll.collider.CompareTag("TARGET_SET"))
        {
            floorRd.material = Mt_success;
            //올바른 행동일 때 플러스 보상을 준다.
            SetReward(+1.0f);
            //학습을 종료시키는 메소드
            EndEpisode();
        }
    }
}
