using System.Collections.Generic;
using UnityEngine;

namespace Eventus.Runtime.Core
{
    [CreateAssetMenu(fileName = "EventusRegistry", menuName = "Eventus/Assets/Categories")]
    public class Categories : ScriptableObject
    {
        public List<string> categories = new() { Global.DEFAULT_CATEGORY };
    }
}