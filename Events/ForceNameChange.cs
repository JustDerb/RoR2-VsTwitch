using RoR2;
using UnityEngine;

namespace VsTwitch
{
    /// <summary>
    /// A <c>CharacterMaster</c> holds a reference to a single <c>CharacterBody</c> object. During stage changes, and other things,
    /// the body may be rotated out for a new one. If this happens, we need to be able to update the stored <c>CharacterBody.baseNameToken</c>
    /// to what we originally had. This class accomplishes that (albeit checking every tick...)
    /// </summary>
    class ForceNameChange : MonoBehaviour
    {
        public string NameToken { get; set; }

        private CharacterMaster body;

        public void Awake()
        {
            body = GetComponent<CharacterMaster>();
        }

        public void Update()
        {
            if (body && body.GetBody() && !body.GetBody().baseNameToken.Equals(NameToken))
            {
                body.GetBody().baseNameToken = NameToken;
            }
        }
    }
}
