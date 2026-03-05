namespace TheLongMood.Afflictions.Patches
{
    internal static class Patches
    {
        [HarmonyPatch(typeof(StatusBar), "Update")]
        private static class TheLongMoodDisplay
        {
            public static double elapsedMinutes = 0d;
            public static GameObject? tempObject;

            private static void Postfix(StatusBar __instance)
            {
                //if (__instance.m_StatusBarType != StatusBar.StatusBarType.Hunger) return;
                if (!__instance.m_IsOnHUD) return;

                if (__instance.m_StatusBarType == StatusBar.StatusBarType.Cold)
                {
                    UpdateTempLabel(__instance);
                }
            }

            private static void UpdateTempLabel(StatusBar __instance)
            {
                if (!Settings.options.Debug)
                {
                    if (tempObject != null)
                    {
                        UnityEngine.Object.Destroy(tempObject);
                        tempObject = null;
                    }
                    return;
                }

                if (tempObject == null)
                {
                    // init
                    UISprite sprite = __instance.m_OuterBoxSprite.GetComponent<UISprite>();
                    GameObject spriteObject = sprite.gameObject;

                    tempObject = new GameObject("TheLongMood");
                    tempObject.transform.SetParent(spriteObject.transform.parent);
                    tempObject.transform.localScale = spriteObject.transform.localScale;

                    UILabel tempLabel = tempObject.AddComponent<UILabel>();
                    tempLabel.text = "TheLongMood";
                    // tempLabel.color = Color.white;
                    tempLabel.color = new Color(0.9f, 0.95f, 1f);  // Use an off white
                    tempLabel.fontStyle = FontStyle.Normal;
                    tempLabel.font = GameManager.GetFontManager().GetUIFontForCharacterSet(CharacterSet.Latin);
                    tempLabel.fontSize = 32;
                    tempLabel.effectStyle = UILabel.Effect.Outline;
                    tempLabel.effectColor = new Color(0.125f, 0.094f, 0.094f, 0.6f);
                    tempLabel.effectDistance = new Vector2(1.7f, 1.7f);

                    tempLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
                    tempLabel.alignment = NGUIText.Alignment.Left;
                    tempLabel.pivot = UIWidget.Pivot.Left;

                    int x_offset = 150 - tempLabel.width;
                    int y_offset = 130 + tempLabel.height;
                    tempObject.transform.localPosition = new Vector3(x_offset, y_offset, 0);
                }
                else if (GameManager.GetHighResolutionTimerManager().GetElapsedMinutes() - elapsedMinutes >= 0.1d)
                {
                    UILabel tempLabel = tempObject.GetComponent<UILabel>();
                    if (tempLabel != null && GameManager.GetFreezingComponent() != null)
                    {
                        if (Core.State.Boredom >= 90)
                        {
                            tempLabel.text = $"b{Core.State.Boredom:F0}%  d{Core.State.Depression:F0}%";
                            tempLabel.color = new Color(0.8f, 0.2f, 0.23f, 1.000f); //darkred
                        }
                        else if (Core.State.Boredom >= 75)
                        {
                            tempLabel.text = $"b{Core.State.Boredom:F0}%  d{Core.State.Depression:F0}%";
                            tempLabel.color = new Color(1f, 0.5f, 0f); //orange
                        }
                        else if (Core.State.Boredom >= 50)
                        {
                            tempLabel.text = $"b{Core.State.Boredom:F0}%  d{Core.State.Depression:F0}%";
                            tempLabel.color = new Color(1f, 0.85f, 0.2f); //yellow
                        }
                        else if (Core.State.Boredom < 50)
                        {
                            tempLabel.text = $"b{Core.State.Boredom:F0}%  d{Core.State.Depression:F0}%";
                            tempLabel.color = new Color(0.9f, 0.95f, 1f); //white
                        }
                            elapsedMinutes = GameManager.GetHighResolutionTimerManager().GetElapsedMinutes();
                    }
                }
            }
        }
    }
}