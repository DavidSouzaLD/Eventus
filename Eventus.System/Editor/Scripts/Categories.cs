using System.Collections.Generic;
using UnityEngine;

namespace Eventus.Editor
{
    [CreateAssetMenu(fileName = "EventusRegistry", menuName = "Eventus/Assets/Categories")]
    public class Categories : ScriptableObject
    {
        public List<string> categories = new() { Global.DEFAULT_CATEGORY };
    }
}