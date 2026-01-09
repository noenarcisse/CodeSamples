using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PrefabPooler : MonoBehaviour
{
    public enum PoolOverflowBehavior { ReturnNull, Grow, RecycleOldest }

    [Header("Instance refs and attributes")]

    [Tooltip("Comportement de dépassement de pool")]
    public PoolOverflowBehavior overflowBehavior;
    [Tooltip("Activer l'element a l'instanciation")]
    public bool activated;
    [Tooltip("Reference du parent auquel on accroche l'obj")]
    public Transform parent;

    [Tooltip("Liste des objets instanciable random")]
    public List<GameObject> pfbAPool;
    [Tooltip("Nombre a pooler")]
    public int nPool;

    public List<GameObject> pfbList;


    void Start()
    {
        pfbList = new List<GameObject>();
        for (int i = 0; i < nPool; i++)
        {
            GameObject obj =  Instantiate(pfbAPool[Random.Range(0, pfbAPool.Count)]);
            obj.transform.SetParent(parent, false);
            obj.SetActive(activated);

            pfbList.Add(obj);
        }
    }

}
