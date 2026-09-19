using System.IO;
using Simu1.Controller;
using Simu1.Pooling;
using Simu1.Targets;
using Simu1.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Simu1.Editor
{
    /// <summary>
    /// Utilidad de Editor para generar automáticamente la interfaz del Simulador Balístico en la escena.
    /// Crea el Canvas, EventSystem con acciones por defecto para Input System,
    /// Panel de controles en la esquina inferior izquierda (Slider de ángulo, InputFields de fuerza y masa, botones de disparo y reinicio),
    /// Modal de Reporte de Tiro en la esquina superior derecha (puntuación, métricas balísticas e impulso de choque, botón de nuevo intento),
    /// y los conecta con la arquitectura MVC (BallisticController y BallisticView).
    /// </summary>
    public static class SimuladorUIMenu
    {
        [MenuItem("Balistica/Crear UI de Simulador en Escena", false, 10)]
        [MenuItem("GameObject/Balistica/Crear UI de Simulador", false, 10)]
        public static void CreateSimulatorUI()
        {
            // 0. Si ya existe un Canvas previo del simulador en la escena, removerlo para regenerar limpio
            GameObject existingCanvas = GameObject.Find("Simulador_Canvas");
            if (existingCanvas == null)
            {
                var panel = GameObject.Find("Panel_Controles");
                if (panel != null)
                {
                    existingCanvas = panel.transform.root.gameObject;
                }
            }
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // 1. Asegurar EventSystem con acciones válidas para el Input System activo
            EventSystem existingES = Object.FindFirstObjectByType<EventSystem>();
            if (existingES == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                var inputModule = eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
#else
                eventSystem.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(eventSystem, "Crear EventSystem");
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                var existingModule = existingES.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (existingModule != null && existingModule.actionsAsset == null)
                {
                    existingModule.AssignDefaultActions();
                    EditorUtility.SetDirty(existingES);
                }
#endif
            }

            // 2. Obtener sprites integrados de Unity para evitar imágenes sliced invisibles sin sprite
            Sprite bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            Sprite standardSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite inputSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd");

            DefaultControls.Resources uiResources = new DefaultControls.Resources
            {
                background = bgSprite,
                standard = standardSprite,
                knob = knobSprite,
                inputField = inputSprite
            };

            // 3. Crear Canvas
            GameObject canvasGO = new GameObject("Simulador_Canvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Crear Canvas Simulador");

            // =========================================================================
            // 4. PANEL DE CONTROLES (Esquina Inferior Izquierda)
            // =========================================================================
            GameObject panelGO = DefaultControls.CreatePanel(uiResources);
            panelGO.name = "Panel_Controles";
            panelGO.transform.SetParent(canvasGO.transform, false);

            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(25f, 25f);
            panelRect.sizeDelta = new Vector2(360f, 540f);

            Image panelImage = panelGO.GetComponent<Image>();
            panelImage.color = new Color(0.10f, 0.13f, 0.18f, 0.94f);

            VerticalLayoutGroup layout = panelGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Título
            CreateLabel(panelGO.transform, "SIMULADOR BALÍSTICO", 18, FontStyle.Bold, new Color(0.95f, 0.95f, 1f));
            CreateSeparator(panelGO.transform);

            // Fila: Ángulo
            CreateLabel(panelGO.transform, "ÁNGULO DE DISPARO:", 13, FontStyle.Bold, Color.white);
            GameObject angleRow = CreateHorizontalRow(panelGO.transform, 32f);

            GameObject sliderGO = DefaultControls.CreateSlider(uiResources);
            sliderGO.name = "Slider_Angulo";
            sliderGO.transform.SetParent(angleRow.transform, false);

            Slider slider = sliderGO.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 90f;
            slider.value = 45f;
            slider.interactable = true;

            LayoutElement sliderLayout = sliderGO.AddComponent<LayoutElement>();
            sliderLayout.minWidth = 180f;
            sliderLayout.preferredWidth = 230f;
            sliderLayout.flexibleWidth = 1f;
            sliderLayout.minHeight = 24f;
            sliderLayout.preferredHeight = 24f;

            Transform bg = sliderGO.transform.Find("Background");
            if (bg != null && bg.TryGetComponent<Image>(out var bgImage))
            {
                bgImage.color = new Color(0.22f, 0.28f, 0.38f, 1f);
                if (bgSprite == null) bgImage.type = Image.Type.Simple;
            }

            Transform fill = sliderGO.transform.Find("Fill Area/Fill");
            if (fill != null && fill.TryGetComponent<Image>(out var fillImage))
            {
                fillImage.color = new Color(0.25f, 0.65f, 1f, 1f);
                if (standardSprite == null) fillImage.type = Image.Type.Simple;
            }

            Transform handle = sliderGO.transform.Find("Handle Slide Area/Handle");
            if (handle != null && handle.TryGetComponent<Image>(out var handleImage))
            {
                handleImage.color = Color.white;
                if (knobSprite == null) handleImage.type = Image.Type.Simple;
                RectTransform handleRect = handle.GetComponent<RectTransform>();
                if (handleRect != null) handleRect.sizeDelta = new Vector2(20f, 20f);
            }

            GameObject angleTextGO = DefaultControls.CreateText(uiResources);
            angleTextGO.name = "Text_AnguloValor";
            angleTextGO.transform.SetParent(angleRow.transform, false);
            Text angleValueText = angleTextGO.GetComponent<Text>();
            angleValueText.text = "45.0°";
            angleValueText.fontSize = 15;
            angleValueText.fontStyle = FontStyle.Bold;
            angleValueText.color = new Color(0.4f, 0.8f, 1f);
            angleValueText.alignment = TextAnchor.MiddleCenter;

            LayoutElement textLayout = angleTextGO.AddComponent<LayoutElement>();
            textLayout.minWidth = 60f;
            textLayout.preferredWidth = 70f;
            textLayout.flexibleWidth = 0f;
            textLayout.minHeight = 24f;
            textLayout.preferredHeight = 24f;

            // Fila: Fuerza
            CreateLabel(panelGO.transform, "FUERZA EN NEWTONS (N):", 13, FontStyle.Bold, Color.white);
            GameObject forceInputGO = DefaultControls.CreateInputField(uiResources);
            forceInputGO.name = "Input_Fuerza";
            forceInputGO.transform.SetParent(panelGO.transform, false);
            forceInputGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 32f);
            InputField forceInput = forceInputGO.GetComponent<InputField>();
            forceInput.contentType = InputField.ContentType.DecimalNumber;
            forceInput.text = "500";

            // Fila: Masa
            CreateLabel(panelGO.transform, "MASA EN KG (kg):", 13, FontStyle.Bold, Color.white);
            GameObject massInputGO = DefaultControls.CreateInputField(uiResources);
            massInputGO.name = "Input_Masa";
            massInputGO.transform.SetParent(panelGO.transform, false);
            massInputGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 32f);
            InputField massInput = massInputGO.GetComponent<InputField>();
            massInput.contentType = InputField.ContentType.DecimalNumber;
            massInput.text = "2.0";

            // Botones de Disparo y Reinicio en el Panel
            GameObject fireBtnGO = CreateCustomButton(panelGO.transform, "Boton_Disparar", "DISPARAR [ESPACIO]", new Color(0.18f, 0.52f, 0.88f, 1f), 36f, uiResources);
            Button panelFireBtn = fireBtnGO.GetComponent<Button>();

            GameObject resetBtnGO = CreateCustomButton(panelGO.transform, "Boton_Restablecer", "RESTABLECER ESCENA", new Color(0.30f, 0.35f, 0.44f, 1f), 30f, uiResources);
            Button panelResetBtn = resetBtnGO.GetComponent<Button>();

            // Card / Panel de Resultados del Último Impacto
            GameObject resultsCard = DefaultControls.CreatePanel(uiResources);
            resultsCard.name = "Card_UltimoImpacto";
            resultsCard.transform.SetParent(panelGO.transform, false);
            resultsCard.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 85f);
            Image resultsImage = resultsCard.GetComponent<Image>();
            resultsImage.color = new Color(0.06f, 0.08f, 0.12f, 0.85f);

            VerticalLayoutGroup resultsLayout = resultsCard.AddComponent<VerticalLayoutGroup>();
            resultsLayout.padding = new RectOffset(10, 10, 8, 8);
            resultsLayout.spacing = 4f;
            resultsLayout.childControlWidth = true;
            resultsLayout.childControlHeight = false;

            CreateLabel(resultsCard.transform, "RESULTADOS ÚLTIMO DISPARO:", 12, FontStyle.Bold, new Color(0.9f, 0.75f, 0.3f));
            Text distText = CreateLabel(resultsCard.transform, "Distancia XZ: --- m", 13, FontStyle.Normal, Color.white);
            Text heightText = CreateLabel(resultsCard.transform, "Altura Máx: --- m", 13, FontStyle.Normal, Color.white);

            // =========================================================================
            // 5. MODAL DE REPORTE DE TIRO (Esquina Superior Derecha)
            // =========================================================================
            GameObject reportPanelGO = DefaultControls.CreatePanel(uiResources);
            reportPanelGO.name = "Modal_ReporteTiro";
            reportPanelGO.transform.SetParent(canvasGO.transform, false);

            RectTransform reportRect = reportPanelGO.GetComponent<RectTransform>();
            reportRect.anchorMin = new Vector2(1f, 1f);
            reportRect.anchorMax = new Vector2(1f, 1f);
            reportRect.pivot = new Vector2(1f, 1f);
            reportRect.anchoredPosition = new Vector2(-25f, -25f);
            reportRect.sizeDelta = new Vector2(530f, 660f);

            Image reportImage = reportPanelGO.GetComponent<Image>();
            reportImage.color = new Color(0.07f, 0.09f, 0.14f, 0.97f);

            VerticalLayoutGroup reportLayout = reportPanelGO.AddComponent<VerticalLayoutGroup>();
            reportLayout.padding = new RectOffset(18, 18, 16, 16);
            reportLayout.spacing = 10f;
            reportLayout.childControlWidth = true;
            reportLayout.childControlHeight = false;
            reportLayout.childForceExpandWidth = true;
            reportLayout.childForceExpandHeight = false;

            // Título técnico
            CreateLabel(reportPanelGO.transform, "REGISTRO TÉCNICO DE ENSAYOS", 18, FontStyle.Bold, new Color(0.4f, 0.8f, 1f), TextAnchor.MiddleCenter);
            CreateSeparator(reportPanelGO.transform);

            // Resumen de estado de adquisición de datos
            GameObject scoreLabelGO = CreateLabel(reportPanelGO.transform, "REGISTRO TÉCNICO: 0 ENSAYOS", 16, FontStyle.Bold, new Color(0.35f, 0.85f, 1f), TextAnchor.MiddleCenter).gameObject;
            scoreLabelGO.name = "Text_PuntuacionReporte";
            Text reportScoreText = scoreLabelGO.GetComponent<Text>();

            // ScrollView para el historial técnico
            GameObject scrollGO = DefaultControls.CreateScrollView(uiResources);
            scrollGO.name = "Scroll_Historial";
            scrollGO.transform.SetParent(reportPanelGO.transform, false);

            LayoutElement scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 420f;
            scrollLayout.preferredHeight = 480f;
            scrollLayout.flexibleHeight = 1f;

            ScrollRect scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            Transform hScrollbar = scrollGO.transform.Find("Scrollbar Horizontal");
            if (hScrollbar != null)
            {
                Object.DestroyImmediate(hScrollbar.gameObject);
            }

            Transform content = scrollGO.transform.Find("Viewport/Content");
            if (content != null)
            {
                VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(12, 12, 12, 12);
                contentLayout.spacing = 8f;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;

                ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            string initialDetails = "Sin ensayos registrados en la sesión actual.\nRealiza un disparo para registrar las magnitudes de impacto.";

            Text reportDetailsText = CreateLabel(content != null ? content : scrollGO.transform, initialDetails, 16, FontStyle.Normal, new Color(0.94f, 0.96f, 1f));
            reportDetailsText.name = "Text_DetallesReporte";
            reportDetailsText.verticalOverflow = VerticalWrapMode.Overflow;
            reportDetailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            reportDetailsText.lineSpacing = 1.35f;

            // Botón de Nuevo Ensayo (conserva historial)
            GameObject modalResetGO = CreateCustomButton(reportPanelGO.transform, "Boton_NuevoIntento", "NUEVO ENSAYO", new Color(0.12f, 0.58f, 0.42f, 1f), 40f, uiResources);
            Button modalResetBtn = modalResetGO.GetComponent<Button>();

            // Botón para limpiar registro
            GameObject clearHistoryGO = CreateCustomButton(reportPanelGO.transform, "Boton_BorrarHistorial", "LIMPIAR REGISTRO", new Color(0.38f, 0.18f, 0.20f, 1f), 32f, uiResources);
            Button clearHistoryBtn = clearHistoryGO.GetComponent<Button>();

            // Iniciar el panel de reporte oculto hasta que concluya un disparo
            reportPanelGO.SetActive(false);

            // =========================================================================
            // 6. Configurar componentes MVC (BallisticView y BallisticController)
            // =========================================================================
            GameObject cannonGO = GameObject.Find("Cannon");
            GameObject targetGO = cannonGO != null ? cannonGO : canvasGO;

            BallisticView ballisticView = targetGO.GetComponent<BallisticView>() ?? targetGO.AddComponent<BallisticView>();
            SerializedObject soView = new SerializedObject(ballisticView);
            soView.FindProperty("angleTextLegacy").objectReferenceValue = angleValueText;
            soView.FindProperty("distanceResultTextLegacy").objectReferenceValue = distText;
            soView.FindProperty("heightResultTextLegacy").objectReferenceValue = heightText;
            soView.FindProperty("shotReportPanel").objectReferenceValue = reportPanelGO;
            soView.FindProperty("reportScoreTextLegacy").objectReferenceValue = reportScoreText;
            soView.FindProperty("reportDetailsTextLegacy").objectReferenceValue = reportDetailsText;
            if (cannonGO != null)
            {
                soView.FindProperty("barrelTransform").objectReferenceValue = cannonGO.transform;
            }
            soView.ApplyModifiedProperties();

            BallisticController ballisticController = targetGO.GetComponent<BallisticController>() ?? targetGO.AddComponent<BallisticController>();
            SerializedObject soCtrl = new SerializedObject(ballisticController);
            soCtrl.FindProperty("view").objectReferenceValue = ballisticView;
            soCtrl.FindProperty("angleSlider").objectReferenceValue = slider;
            soCtrl.FindProperty("forceInputField").objectReferenceValue = forceInput;
            soCtrl.FindProperty("massInputField").objectReferenceValue = massInput;
            soCtrl.FindProperty("fireButton").objectReferenceValue = panelFireBtn;
            soCtrl.FindProperty("resetButton").objectReferenceValue = modalResetBtn;

            var panelResetProp = soCtrl.FindProperty("panelResetButton");
            if (panelResetProp != null)
            {
                panelResetProp.objectReferenceValue = panelResetBtn;
            }

            var clearHistProp = soCtrl.FindProperty("clearHistoryButton");
            if (clearHistProp != null)
            {
                clearHistProp.objectReferenceValue = clearHistoryBtn;
            }

            TargetStructureManager structureManager = Object.FindFirstObjectByType<TargetStructureManager>();
            if (structureManager != null)
            {
                soCtrl.FindProperty("targetStructureManager").objectReferenceValue = structureManager;
            }

            if (cannonGO != null)
            {
                soCtrl.FindProperty("barrelTransform").objectReferenceValue = cannonGO.transform;
                Transform spawn = cannonGO.transform.Find("spawnPoint");
                if (spawn != null)
                {
                    soCtrl.FindProperty("spawnPoint").objectReferenceValue = spawn;
                }
                ProjectilePool pool = cannonGO.GetComponentInChildren<ProjectilePool>();
                if (pool != null)
                {
                    soCtrl.FindProperty("projectilePool").objectReferenceValue = pool;
                }
            }
            soCtrl.ApplyModifiedProperties();

            // 7. Guardar como Prefab reutilizable
            string prefabDir = "Assets/Prefab";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
            }
            string prefabPath = Path.Combine(prefabDir, "SimuladorUI.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);

            Selection.activeGameObject = canvasGO;
            Debug.Log($"[Simulador Balístico] Interfaz actualizada con éxito y guardada como prefab en '{prefabPath}'.");
        }

        private static Text CreateLabel(Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            DefaultControls.Resources uiResources = new DefaultControls.Resources();
            GameObject go = DefaultControls.CreateText(uiResources);
            go.name = "Label_" + text.Replace(" ", "_").Replace(":", "");
            go.transform.SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = alignment;

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, fontSize + 8);
            return t;
        }

        private static GameObject CreateHorizontalRow(Transform parent, float height)
        {
            GameObject row = new GameObject("Row");
            row.transform.SetParent(parent, false);

            RectTransform rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, height);

            HorizontalLayoutGroup hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = height;
            rowLayout.preferredHeight = height;
            rowLayout.flexibleWidth = 1f;

            return row;
        }

        private static void CreateSeparator(Transform parent)
        {
            GameObject sep = new GameObject("Separator");
            sep.transform.SetParent(parent, false);

            RectTransform rect = sep.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 2f);

            Image img = sep.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.15f);
        }

        private static GameObject CreateCustomButton(Transform parent, string name, string labelText, Color bgColor, float height, DefaultControls.Resources uiResources)
        {
            GameObject btnGO = DefaultControls.CreateButton(uiResources);
            btnGO.name = name;
            btnGO.transform.SetParent(parent, false);

            RectTransform rect = btnGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);

            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;

            Image img = btnGO.GetComponent<Image>();
            if (img != null)
            {
                img.color = bgColor;
            }

            Text txt = btnGO.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = labelText;
                txt.fontSize = 14;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
            }

            return btnGO;
        }
    }
}
