using System.Collections.Generic;
using UnityEngine;

public class KeepOneDestroyOthers : MonoBehaviour
{
    [Header("Lista de objetos entre los que se elegirá 1")]
    public List<GameObject> objects;

    void Start()
    {
        KeepOnlyOne();
    }

    public void KeepOnlyOne()
    {
        if (objects == null || objects.Count == 0)
        {
            Debug.LogWarning("La lista de objetos está vacía.");
            return;
        }

        // Elegimos un índice al azar
        int indexToKeep = Random.Range(0, objects.Count);

        // Guardamos el objeto que se queda (opcional, por si lo usas después)
        GameObject objectToKeep = objects[indexToKeep];

        // Recorremos todos los objetos y destruimos los que no son el elegido
        for (int i = 0; i < objects.Count; i++)
        {
            if (i != indexToKeep && objects[i] != null)
            {
                Destroy(objects[i]);
            }
        }


    }
}
