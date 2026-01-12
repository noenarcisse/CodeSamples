using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Crowd behavior system. Script is attached to the parent where the crowd will be populated.
/// </summary>
public class Crowd : MonoBehaviour
{

    public Vector3 spawnPos;

    public Transform clickCursor;

    public int i, j;
    public GameObject[,] crowd;
    public GameObject[,] cases;
    public OnMouseHover[,] mouseHoverScripts;

    public Vector2 rand;
    public float mid1, mid2;
    public Bounds box;


    public GameObject CasesParent;
    public GameObject NoctuleInstanceRef, CaseInstanceRef;
    public GameObject NoctulesParent;



    void Start()
    {
        crowd = new GameObject[i, j];
        cases = new GameObject[i, j];
        mouseHoverScripts = new OnMouseHover[i, j];


        int b = 0;

        mid1 = i / 2;
        mid2 = j / 2;

        for (int a = 0; a < i; a++)
        {
            while (b < j)
            {
                GameObject noct = Instantiate(NoctuleInstanceRef, spawnPos, Quaternion.identity, NoctulesParent.transform);
                GameObject c = Instantiate(CaseInstanceRef, spawnPos, Quaternion.identity, CasesParent.transform);


                c.transform.position = new Vector3(Random.Range(rand.x * Mathf.Abs(a - mid1), rand.y * Mathf.Abs(a - mid1)) + a, 0f, Random.Range(rand.x * Mathf.Abs(b - mid2), rand.y * Mathf.Abs(b - mid2)) + b);
                c.name = "Case" + a + " " + b;

                noct.transform.position = spawnPos;
                noct.name = "Noctule " + a + " " + b;
                noct.GetComponent<NavMeshAgent>().enabled = true;
                noct.GetComponent<FollowCaseCrowd>().cible = c;

                if (noct.TryGetComponent<OnMouseHover>(out OnMouseHover omhs))
                {
                    mouseHoverScripts[a, b] = omhs;
                }

                crowd[a, b] = noct;
                cases[a, b] = c;

                b++;
            }
            b = 0;
        }

        //calcule du bound du groupe de noct post deplacement/random
        box = new Bounds(this.transform.GetChild(0).position, Vector3.zero);

        List<Renderer> renders = new List<Renderer>();
        renders.AddRange(this.GetComponentsInChildren<Renderer>());

        if (renders.Count > 0)
        {
            foreach (Renderer r in renders)
            {
                box.Encapsulate(r.bounds);
            }
        }

        //recalcule d'offset pour que le curseur laisse l'impression d'etre au centre du groupe quand on clique
        transform.GetChild(0).position -= box.extents;
        transform.GetChild(0).position += CaseInstanceRef.GetComponent<MeshRenderer>().bounds.extents;


        float s = Random.Range(0.5f, 1f);
        IEnumerator corout = TickRandomPlace(s);
        StartCoroutine(corout);
    }

    void Update()
    {
        Teleport(clickCursor.position);
    }

    /// <summary>
    /// Teleport the crowd object to target position.
    /// </summary>
    /// <param name="worldPos">The position in world space</param>
    /// <returns>true if the teleport was applied, false otherwise</returns>
    public bool Teleport(Vector3 worldPos)
    {
        if (this.transform.position != worldPos)
        {
            this.transform.position = worldPos;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Modify placement of units after x seconds then loop.
    /// </summary>
    /// <param name="s">Delay to wait in seconds</param>
    /// <returns>WaitForSeconds coroutine</returns>
    public IEnumerator TickRandomPlace(float s)
    {
        while (true)
        {
            RandomPlace();
            yield return new WaitForSeconds(s);
        }

    }

    /// <summary>
    /// Randomize all placements of the crowd units.
    /// </summary>
    public void RandomPlace()
    {

        int b = 0;
        for (int a = 0; a < i; a++)
        {
            while (b < j)
            {
                //on ne lance un rand que si le noctule n'est pas cible par le joueur qui regarde un truc dessus
                if (!mouseHoverScripts[a, b].isMouseOver)
                {
                    cases[a, b].transform.localPosition = new Vector3(Random.Range(rand.x * Mathf.Abs(a - mid1), rand.y * Mathf.Abs(a - mid1)) + a, 0f, Random.Range(rand.x * Mathf.Abs(b - mid2), rand.y * Mathf.Abs(b - mid2)) + b);
                }
                b++;
            }
            b = 0;
        }

    }
}




