# Eventus: The Unified Communication Hub
Version: 1.0.0

1. Philosophy
Eventus is a professional, centralized communication system designed to create highly decoupled and dynamic architectures in your Unity project. It eliminates the need for direct component references, preventing "spaghetti code" and making your project cleaner, more scalable, and easier to maintain.

Eventus masterfully combines two essential communication patterns into a single, elegant API:

Event Bus (Push System): For reactive, notification-based communication. Components Publish an event (e.g., OnPlayerCrashed) to announce that something has happened, and any interested component can Subscribe to react to it. This is ideal for actions and one-time occurrences.

Data Hub (Blackboard / Pull System): For shared, real-time state. Components Write data (e.g., current motorcycle speed) to a central "blackboard," and any other component can Read that data at any time. This is perfect for values that change every frame, like UI data.
The entire system is managed through the intuitive Eventus Hub editor window, giving you full visual control over your project's communication channels without ever needing to manually edit the underlying code.



```
using Eventus.Runtime;
using UnityEngine;

public class Player : MonoBehaviour
{
    private int _health = 100;

    void Start()
    {
        // Write initial health to the Data Hub
        Eventus2.Write<int>(Channel.PlayerHealth, _health);
        
        // Subscribe to a damage event
        Eventus2.Subscribe<int>(Channel.OnTakeDamage, TakeDamage);
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks!
        Eventus2.Unsubscribe<int>(Channel.OnTakeDamage, TakeDamage);
    }

    private void TakeDamage(int amount)
    {
        _health -= amount;
        // Update the value in the Data Hub for other systems to read
        Eventus2.Write<int>(Channel.PlayerHealth, _health);
    }
}
```
