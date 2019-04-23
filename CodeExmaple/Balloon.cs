using System.Collections;
using UnityEngine;

using Random = UnityEngine.Random;

public class Balloon : MonoBehaviour
{   
	public StatisticsData sData;

	public static Balloon instance;

    private Rigidbody2D rb2d;

    //MISSES
	public GameObject missIndicator;
	public short nearBombs;
	public GameObject lastNearBomb;
    public GameObject lastNearRocket;
    public GameObject currentBomb;
	public int coefNearBomb = 1;

	public bool nearBomb;
    public bool nearRocket;

    //ITEMS
    public GameObject itemsBalloon;
    public GameObject magnet;

    //AUDIO
    public AudioClip coinSound;
    public AudioClip bombSound;
    public AudioClip lightningSound;
    public AudioClip birdSound;
    public AudioClip rocketFlingSound;
    public AudioClip rockectExSound;

    //LIFES
    public int lifePerLife = 1;
    public GameObject[] lifes;
    public GameObject[] lifesBg;
    public int maxLifes;

    public int coinPerCoin;

    void Awake()
    {
		if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Update(){
        itemsBalloon.transform.position = GameController.instance.gameBalloon.transform.position;
	}

    void OnTriggerEnter2D(Collider2D collider2d)
    {
        if(collider2d.CompareTag("UpsideTeleport") && !GameController.instance.isUpside)
        {
            StartCoroutine(GameController.instance.TeleportUpsideStage());
            Debug.Log("Collided");
        }

        if (collider2d.CompareTag("UpsideCoin"))
        {
            collider2d.GetComponent<Animator>().SetTrigger("Play");
            sData.coinsPR += 30;
            sData.coinsPQ += 30;
            sData.mushroomsCollectedPQ++;
        }

        if (collider2d.CompareTag("Cloud") || collider2d.CompareTag("UpsideCloud"))
        {
            int coef = 0;
            if (transform.position.x >= collider2d.gameObject.transform.position.x)
                coef = -1;
            else
                coef = 1;

            StartCoroutine(PushCloud(collider2d.gameObject, coef));

            if(collider2d.CompareTag("Cloud"))
                sData.cloudsPushedPQ++;
            if (collider2d.CompareTag("UpsideCloud"))
                sData.leembPushedPQ++;

        }

        if (collider2d.CompareTag("BombPassedLine"))
        {

            if (!nearBomb)
            {
                coefNearBomb = 1;

                if (sData.maxNearBombInRowPR < nearBombs)
                    sData.maxNearBombInRowPR = nearBombs;

                nearBombs = 0;

            }

            if (GameController.instance.isAlive && nearBomb)
            {
                //Debug.Log("NearBomb");
                if (QuestManager.instance.isShowBombMiss && lastNearBomb != null)
                {
                    if (GameController.instance.gameBalloon.transform.position.x > lastNearBomb.transform.position.x)
                    {
                        GameObject tempMiss = Instantiate(missIndicator, new Vector2(lastNearBomb.transform.position.x + 1.8f, lastNearBomb.transform.position.y), Quaternion.identity);
                        Destroy(tempMiss, 1f);
                    }
                    if (GameController.instance.gameBalloon.transform.position.x < lastNearBomb.transform.position.x)
                    {
                        GameObject tempMiss = Instantiate(missIndicator, new Vector2(lastNearBomb.transform.position.x - 1.8f, lastNearBomb.transform.position.y), Quaternion.identity);
                        Destroy(tempMiss, 1f);
                    }
                }

                //GameController.instance.flyingSpeed += .05f;
                //GameController.instance.currentSpeed += .05f; ;


                nearBombs++;
                sData.nearBombPR++;
                sData.nearBombPQ++;
                nearBomb = false;

                coefNearBomb *= 2;
            }

        }

        if (collider2d.CompareTag("Gradient2") && !GameController.instance.god)
            GameController.instance.isAlive = false;

        if(collider2d.CompareTag("UpsideObstacle"))
        {
            if (GameController.instance.god == false)
                CheckLife();
        }

        if (collider2d.gameObject.tag == "bomb" && GameController.instance.isAlive == true && collider2d.gameObject != currentBomb)
        {
            if (!collider2d.GetComponent<BombScript>().exploded)
            {
                collider2d.GetComponent<BombScript>().exploded = true;
                currentBomb = collider2d.gameObject;
                sData.bombExplodedPR += 1;
                sData.bombExplodedPQ++;
                //AudioManager.instance.Play("Bomb");
                if (AudioManager.instance.soundEnabled)
                    GetComponent<AudioSource>().PlayOneShot(bombSound, .6f);

                if (GameController.instance.god == false)
                {
                    CheckLife();
                }
            }

        }

        if(collider2d.CompareTag("Life"))
        {
            AddLife(lifePerLife);
            GameController.instance.lifeRate = (int)GameController.instance.score + Random.Range(90, 200) + GameController.instance.lifeRemain * 80;
            GameController.instance.canSpawnLife = true;
            GameController.instance.lifeShield.SetActive(true);
            GameObject splash = Instantiate(GameController.instance.lifeSplash, new Vector2(collider2d.transform.position.x, collider2d.transform.position.y), Quaternion.identity);
            Destroy(splash, 1f);
            Destroy(collider2d.gameObject);
            Debug.Log("Life++ = " + GameController.instance.lifeRemain.ToString());

        }

        if (collider2d.CompareTag("NearBomb") && GameController.instance.gamePlay)
        {  
            lastNearBomb = collider2d.gameObject;
            nearBomb = true;

        }
        if (collider2d.CompareTag("NearRocket") && GameController.instance.gamePlay)
        {
            StartCoroutine(NearRocketCheck());
            lastNearRocket = collider2d.gameObject;

        }
        if (collider2d.gameObject.tag == "Rocket")
        {
            if (GameController.instance.isAlive)
            {
                if (AudioManager.instance.soundEnabled)
                    GetComponent<AudioSource>().PlayOneShot(rockectExSound, .6f);

                if (GameController.instance.god == false)
                {
                    CheckLife();
                }
            }
        }

        if (collider2d.CompareTag("NearPlane1"))
            StartCoroutine(WaitSecPlane());


        if (collider2d.gameObject.tag == "Coin")
        {

            
            if(AudioManager.instance.soundEnabled)
                GetComponent<AudioSource>().PlayOneShot(coinSound, .6f);

            sData.coinsPR += coinPerCoin;
            sData.coinsPQ += coinPerCoin;

            if (StagesController.instance.onGradient)
            {
                switch (StagesController.instance.currentGradient)
                {
                    case 1:
                        sData.coinPickedUpPR_GR1++;
                        break;
                    case 2:
                        sData.coinPickedUpPR_GR2++;
                        break;
                    case 3:
                        sData.coinPickedUpPR_GR3++;
                        break;
                    case 4:
                        sData.coinPickedUpPR_GR4++;
                        break;
                }
            }
            collider2d.GetComponent<Animator>().SetTrigger("CoinGet");
            Destroy(collider2d.gameObject, 1.5f);
        }

    }


    public void AddLife(int amount)
    {
        if (GameController.instance.lifeRemain != maxLifes)
        {
            if (GameController.instance.lifeRemain + amount <= maxLifes)
            {
                GameController.instance.lifeRemain += amount;
                for (int i = 0; i < GameController.instance.lifeRemain; i++)
                    lifes[i].SetActive(true);
            }
            else
            {

                GameController.instance.lifeRemain = maxLifes - GameController.instance.lifeRemain;
                for (int i = 0; i < GameController.instance.lifeRemain; i++)
                    lifes[i].SetActive(true);

            }
        }
    }

    public void RemoveLife()
    {
        if(GameController.instance.lifeRemain >= 1)
        {
            lifes[GameController.instance.lifeRemain - 1].SetActive(false);
            GameController.instance.lifeRemain--;
        }
    }

    public void CheckLife()
    {
        if (GameController.instance.isUpside)
        {
            GameController.instance.reviveCounter = 0;
            GameController.instance.isAlive = false;
            GameController.instance.gamePlay = false;
        }
        else
        {
            Debug.Log(GameController.instance.lifeRemain);
            if (GameController.instance.lifeRemain == 0)
            {
                GameController.instance.isAlive = false;
                GameController.instance.gamePlay = false;
            }
            if (GameController.instance.lifeRemain > 0)
            {
                GameController.instance.lifeRate = (int)GameController.instance.score + Random.Range(30, 120) + (GameController.instance.lifeRemain - 1) * 50;
                GameController.instance.canSpawnLife = true;
                lifes[GameController.instance.lifeRemain - 1].SetActive(false);
                GameController.instance.lifeRemain--;

            }

            if (GameController.instance.lifeRemain == 0)
                GameController.instance.lifeShield.SetActive(false);
        }
    }



    void OnParticleCollision(GameObject other)
    {
        Debug.Log("OUCHHH!!!");
        if (GameController.instance.isAlive && other.CompareTag("MeteoritThing") && !GameController.instance.god)
        {
            CheckLife();
        }
    }

    IEnumerator NearRocketCheck()
    {
        float startScore = GameController.instance.score;
        while (GameController.instance.score < startScore + 1.7f)
        {
            yield return null;
        }
        if (GameController.instance.isAlive)
        {


            sData.nearRocketsPQ++;
            sData.nearRocketPR++;

            if (QuestManager.instance.isShowRocketMiss && lastNearRocket != null)
            {
                if (GameController.instance.gameBalloon.transform.position.x > lastNearRocket.transform.position.x)
                {
                    GameObject tempMiss = Instantiate(missIndicator, new Vector2(lastNearRocket.transform.position.x + 1.5f, lastNearRocket.transform.position.y + 2f), Quaternion.identity);
                    Destroy(tempMiss, 1f);
                }
                if (GameController.instance.gameBalloon.transform.position.x < lastNearRocket.transform.position.x)
                {
                    GameObject tempMiss = Instantiate(missIndicator, new Vector2(lastNearRocket.transform.position.x - 1.5f, lastNearRocket.transform.position.y + 2f), Quaternion.identity);
                    Destroy(tempMiss, 1f);
                }
            }
        }
    }

    IEnumerator PushCloud(GameObject cloud, int coef)
    {
        float timer = .275f;

        while (timer > 0 && cloud != null)
        {
            timer -= Time.deltaTime;
            Vector2 pos = cloud.transform.position;
            pos.x += 5.5f * coef * Time.deltaTime;
            cloud.transform.position = pos;
            yield return null;
        }
        timer = .1f;
        while (timer > 0 && cloud != null)
        {
            timer -= Time.deltaTime;
            Vector2 pos = cloud.transform.position;
            pos.x += 2.5f * coef * Time.deltaTime;
            cloud.transform.position = pos;
            yield return null;
        }
        timer = .2f;
        while (timer > 0 && cloud != null)
        {
            timer -= Time.deltaTime;
            Vector2 pos = cloud.transform.position;
            pos.x += 1f * coef * Time.deltaTime;
            cloud.transform.position = pos;
            yield return null;
        }
    }


    IEnumerator WaitSecPlane()
    {

        yield return new WaitForSeconds(1.4f);

        if (GameController.instance.isAlive)
        {
            //Debug.Log("NearPlane"); 
            sData.nearPlanesPR++;
            sData.nearPlanesPQ++;
        }
    }

}



