using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

[assembly: MelonInfo(typeof(gameFuker.Core), "gameFuker", "1.0.0", "xbox", null)]
[assembly: MelonGame("Stonehollow Workshop LLC", "Eterspire")]

namespace gameFuker
{
    public class Core : MelonMod
    {
        // We'll store references to the player components here
        private Il2Cpp.LocalPlayerController _localPlayerController;
        private Il2Cpp.PlayerAnimationManager _animManager;
        private GameObject _mobContainer; // Cached reference to the mob container
        private GameObject _sellAllButton; // Cached reference to the sell all button
        private GameObject _sellButtonsContainer; // Cached reference for the sell buttons
        private GameObject _equipableButtonsContainer; // Cached reference for the equipable buttons

        // New variables for the mob freeze toggle
        private bool _mobsFrozen = false;
        private Vector3 _freezePosition;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }

        // This method is called by MelonLoader every frame.
        public override void OnUpdate()
        {
            // --- Hotkey Logic: Toggle Inventory UI ---
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // The full path to the GameObject
                string inventoryPath = "GameControllers(Clone)/Canvas/General UI/(UI) Mobile UI Menus/(XO) Inventory Half Menu";
                GameObject inventoryUI = GameObject.Find(inventoryPath);

                if (inventoryUI != null)
                {
                    // Toggle the active state
                    bool isCurrentlyActive = inventoryUI.activeSelf;
                    inventoryUI.SetActive(!isCurrentlyActive);

                    // Log the action
                    LoggerInstance.Msg(!isCurrentlyActive
                        ? "F2 pressed! Opening Inventory Sell UI."
                        : "F2 pressed! Closing Inventory Sell UI.");
                }
                else
                {
                    LoggerInstance.Warning("Could not find the Inventory Sell UI element. Path may be incorrect or UI not loaded.");
                }
            }

            // --- NEW HOTKEY: Vacuum "E3 Warp" objects ---
            if (Input.GetKeyDown(KeyCode.F3))
            {
                VacuumObjects("E3 Warp", true); // CHANGED: Now uses 'starts with' logic
            }

            // --- NEW HOTKEY: Vacuum "(NPC)" objects ---
            if (Input.GetKeyDown(KeyCode.F4))
            {
                VacuumObjects("(NPC)", true); // true = name starts with
            }

            // --- Constantly keep the UI buttons active ---
            if (_sellAllButton != null && !_sellAllButton.activeSelf)
            {
                _sellAllButton.SetActive(true);
            }

            if (_sellButtonsContainer != null && !_sellButtonsContainer.activeSelf)
            {
                _sellButtonsContainer.SetActive(true);
            }

            if (_equipableButtonsContainer != null && !_equipableButtonsContainer.activeSelf)
            {
                _equipableButtonsContainer.SetActive(true);
            }

            // Only run our logic if we have successfully found the components.
            if (_localPlayerController != null)
            {
                // --- Logic to set immunity (applied every frame) ---
                if (!_localPlayerController.IsImmuneToMobDamage())
                {
                    _localPlayerController.SetImmuneToMobDamage(true);
                }

                // --- Hotkey Logic: Toggle Mob Freeze ---
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    // Invert the boolean toggle
                    _mobsFrozen = !_mobsFrozen;

                    if (_mobsFrozen)
                    {
                        // When we first enable the freeze, calculate and store the position.
                        _freezePosition = _localPlayerController.transform.position + (_localPlayerController.transform.forward * 3f);
                        LoggerInstance.Msg("F1 pressed! Mob freeze has been ENABLED.");
                    }
                    else
                    {
                        LoggerInstance.Msg("F1 pressed! Mob freeze has been DISABLED.");
                    }
                }

                // --- Mob Freeze Logic (runs every frame if toggled on) ---
                if (_mobsFrozen)
                {
                    // Use the cached reference to the mob container
                    if (_mobContainer != null)
                    {
                        // --- FIX: Use a for loop instead of foreach for Il2Cpp compatibility ---
                        for (int i = 0; i < _mobContainer.transform.childCount; i++)
                        {
                            Transform mobTransform = _mobContainer.transform.GetChild(i);
                            if (mobTransform != null)
                            {
                                mobTransform.position = _freezePosition;
                            }
                        }
                    }
                }
            }

            if (_animManager != null)
            {
                // --- Logic for attack cooldown (applied every frame) ---
                if (_animManager.GetAttackCooldown() != 0f)
                {
                    _animManager.SetAttackCooldown(0f);
                }
            }
        }

        /// <summary>
        /// Finds objects by name and teleports them in a line in front of the player.
        /// </summary>
        /// <param name="nameIdentifier">The name or starting part of the name to search for.</param>
        /// <param name="useStartsWith">If true, matches objects whose name starts with the identifier. If false, requires an exact match.</param>
        private void VacuumObjects(string nameIdentifier, bool useStartsWith)
        {
            if (_localPlayerController == null)
            {
                LoggerInstance.Warning($"Cannot vacuum '{nameIdentifier}' objects, LocalPlayerController not found!");
                return;
            }

            LoggerInstance.Msg($"Hotkey pressed! Searching for objects with name identifier: '{nameIdentifier}'.");

            var foundObjects = new List<GameObject>();
            var allGameObjects = GameObject.FindObjectsOfType<GameObject>();

            foreach (var go in allGameObjects)
            {
                bool isMatch = useStartsWith ? go.name.StartsWith(nameIdentifier) : go.name == nameIdentifier;
                if (isMatch)
                {
                    foundObjects.Add(go);
                }
            }

            if (foundObjects.Count == 0)
            {
                LoggerInstance.Msg($"Found no objects to vacuum with identifier: '{nameIdentifier}'.");
                return;
            }

            LoggerInstance.Msg($"Found {foundObjects.Count} objects. Teleporting them now.");

            // CHANGED: Reduced distance from 5f to 3f to bring them closer
            Vector3 basePosition = _localPlayerController.transform.position + (_localPlayerController.transform.forward * 3f);
            float spacing = 2.0f; // How far apart to space the objects

            for (int i = 0; i < foundObjects.Count; i++)
            {
                GameObject objToMove = foundObjects[i];
                // Calculate a new position in a line to the player's right
                Vector3 newPosition = basePosition + (_localPlayerController.transform.right * i * spacing);
                objToMove.transform.position = newPosition;
            }
        }


        // This method is called by MelonLoader every time a new scene is finished loading.
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg($"Scene '{sceneName}' with build index {buildIndex} has been loaded!");

            // When a scene loads, we'll try to find our components and store them.
            _localPlayerController = GameObject.FindObjectOfType<Il2Cpp.LocalPlayerController>();
            _animManager = GameObject.FindObjectOfType<Il2Cpp.PlayerAnimationManager>();
            _mobContainer = null; // Reset on new scene load
            _sellAllButton = null; // Reset on new scene load
            _sellButtonsContainer = null; // Reset on new scene load
            _equipableButtonsContainer = null; // Reset on new scene load

            // Log whether we found them or not.
            if (_localPlayerController != null)
            {
                LoggerInstance.Msg("Successfully found and cached the LocalPlayerController!");
            }
            else
            {
                LoggerInstance.Msg("LocalPlayerController was not found in this scene.");
            }

            if (_animManager != null)
            {
                LoggerInstance.Msg("Successfully found and cached the PlayerAnimationManager!");
            }
            else
            {
                LoggerInstance.Msg("PlayerAnimationManager was not found in this scene.");
            }

            // Find all transforms in the scene, including inactive ones, to locate our objects
            var allSceneTransforms = Resources.FindObjectsOfTypeAll<Transform>();

            // It's more efficient to loop once and check for all our objects
            foreach (var t in allSceneTransforms)
            {
                if (_mobContainer == null && t.gameObject.name.StartsWith("Mob Container (Contains"))
                {
                    _mobContainer = t.gameObject;
                    LoggerInstance.Msg("Successfully found and cached the Mob Container!");
                }

                if (_sellAllButton == null && t.gameObject.name == "Sell All Misc Items Shop Button Container")
                {
                    _sellAllButton = t.gameObject;
                    LoggerInstance.Msg("Successfully found and cached the 'Sell All Misc Items' button.");
                }

                if (_sellButtonsContainer == null && t.gameObject.name == "Sell Shop Buttons Container")
                {
                    _sellButtonsContainer = t.gameObject;
                    LoggerInstance.Msg("Successfully found and cached the 'Sell Shop Buttons Container'.");
                }

                if (_equipableButtonsContainer == null && t.gameObject.name == "Equipable Buttons Container")
                {
                    _equipableButtonsContainer = t.gameObject;
                    LoggerInstance.Msg("Successfully found and cached the 'Equipable Buttons Container'.");
                }
            }

            // Logging for what wasn't found
            if (_mobContainer == null)
            {
                LoggerInstance.Warning("Mob Container was not found in this scene.");
            }
            if (_sellAllButton == null)
            {
                LoggerInstance.Warning("'Sell All Misc Items' button was not found in this scene.");
            }
            if (_sellButtonsContainer == null)
            {
                LoggerInstance.Warning("'Sell Shop Buttons Container' was not found in this scene.");
            }
            if (_equipableButtonsContainer == null)
            {
                LoggerInstance.Warning("'Equipable Buttons Container' was not found in this scene.");
            }
        }
    }
}

