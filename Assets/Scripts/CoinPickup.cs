using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CoinPickup : MonoBehaviour {
    private int coins;

    public TextMeshProUGUI coinText;
    private PlayerController playerController;

    private void Start() {
        
    }

    private void OnTriggerEnter(Collider other) {
        
        playerController = GetComponent<PlayerController>();
        
        //character flipped and coin flipped
        if (playerController.is2d && other.CompareTag("CoinFlat")) {
            Destroy(other.gameObject);
            coins++;

            coinText.text = coins.ToString();

        }

        //character not flipped coin not flipped
        if (!playerController.is2d && other.CompareTag("CoinFlipped")) {
            Destroy(other.gameObject);
            coins++;

            coinText.text = coins.ToString();

        }

    }
}
