using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IceCream : MonoBehaviour
{
    [SerializeField] private Sprite[] rozhoks;
    [SerializeField] private Sprite[] creams;
    [SerializeField] private Image[] images;

    public List<int> types = new List<int>();

    public void UpdateView()
    {
        for(int i = 0; i < images.Length; i++)
        {
            images[i].enabled = types.Count > i;
            
            if(types.Count > i)
            {
                if(i == 0) images[i].sprite = rozhoks[types[i]];
                else images[i].sprite = creams[types[i]];
            }
        }
    }

    public void SetRandom(int _level, int customerId)
    {
        types.Clear();
        int count = Mathf.Min(4, _level / 10 + (customerId % 3 == 0 && customerId > 0? 3 : 2));

        types.Add(Random.Range(0, 2));

        for(int i = 1; i < count; i++) types.Add(Random.Range(0, 4));

        UpdateView();
    }
}
