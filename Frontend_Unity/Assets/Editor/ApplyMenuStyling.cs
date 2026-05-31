using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public static class ApplyMenuStyling
{
    [MenuItem("Tools/Apply Menu Styling")]
    public static void ApplyStyles()
    {
        // 1. Find main Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[ApplyMenuStyling] Canvas not found in active scene!");
            return;
        }

        // 2. Find PanelMenu under Canvas (even if it's inactive)
        Transform panelMenuTrans = canvas.transform.Find("PanelMenu");
        if (panelMenuTrans == null)
        {
            Debug.LogError("[ApplyMenuStyling] PanelMenu not found under Canvas!");
            return;
        }
        GameObject panelMenu = panelMenuTrans.gameObject;

        // 3. Make sure PanelMenu is active and other panels are inactive for instant editor visibility
        panelMenu.SetActive(true);
        
        Transform panelEntrevista = canvas.transform.Find("PanelEntrevista");
        if (panelEntrevista != null) panelEntrevista.gameObject.SetActive(false);
        
        Transform panelReporte = canvas.transform.Find("PanelReporte");
        if (panelReporte != null) panelReporte.gameObject.SetActive(false);
        
        Transform panelHistorial = canvas.transform.Find("PanelHistorial");
        if (panelHistorial != null) panelHistorial.gameObject.SetActive(false);

        // Hide 3D environment in editor scene view to show the menu cleanly
        Transform entorno3D = GameObject.Find("Entorno3D")?.transform;
        if (entorno3D != null) entorno3D.gameObject.SetActive(false);

        // Load premium font asset
        string fontPath = "Assets/Fonts/GOOGLESANS-ITALIC-VARIABLEFONT_GRAD,OPSZ,WGHT SDF.asset";
        TMP_FontAsset googleSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (googleSans == null)
        {
            Debug.LogWarning("[ApplyMenuStyling] Google Sans font not found. Using default TMPro font.");
        }

        // 4. PanelMenu (Background Office image styling with deep premium slate overlay)
        Image bgImage = panelMenu.GetComponent<Image>();
        if (bgImage != null)
        {
            // Rich semi-transparent dark slate-blue to filter the bright office photo
            bgImage.color = new Color(0.07f, 0.10f, 0.16f, 0.76f);
        }

        // 5. Title Styling (Titulo - perfectly centered above the card)
        Transform titleTransform = panelMenu.transform.Find("Titulo");
        if (titleTransform != null)
        {
            RectTransform titleRt = titleTransform.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, -95f);
            titleRt.sizeDelta = new Vector2(1000f, 90f);

            var titleTxt = titleTransform.GetComponent<TextMeshProUGUI>();
            if (titleTxt != null)
            {
                titleTxt.text = "SIMULADOR DE ENTREVISTAS";
                if (googleSans != null) titleTxt.font = googleSans;
                titleTxt.fontSize = 54f;
                titleTxt.color = Color.white;
                titleTxt.fontStyle = FontStyles.Bold;
                titleTxt.alignment = TextAlignmentOptions.Center;
                titleTxt.characterSpacing = 3.5f;
            }

            // Create/style an elegant accent separator below the title
            Transform separator = panelMenu.transform.Find("TitleSeparator");
            if (separator == null)
            {
                GameObject sepObj = new GameObject("TitleSeparator", typeof(RectTransform), typeof(Image));
                sepObj.transform.SetParent(panelMenu.transform, false);
                separator = sepObj.transform;
            }
            
            RectTransform sepRt = separator.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0.5f, 1f);
            sepRt.anchorMax = new Vector2(0.5f, 1f);
            sepRt.pivot = new Vector2(0.5f, 0.5f);
            sepRt.anchoredPosition = new Vector2(0f, -145f);
            sepRt.sizeDelta = new Vector2(200f, 2.5f);

            Image sepImg = separator.GetComponent<Image>();
            if (sepImg != null)
            {
                // Soft teal accent color for a futuristic high-tech separator line
                sepImg.color = new Color(0.08f, 0.72f, 0.65f, 0.70f);
            }
        }

        // 6. CajaMenu Card Styling
        Transform cajaMenuTransform = panelMenu.transform.Find("CajaMenu");
        if (cajaMenuTransform != null)
        {
            RectTransform cardRt = cajaMenuTransform.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, -30f);
            cardRt.sizeDelta = new Vector2(500f, 620f); // Sleek vertical card form

            Image cardImg = cajaMenuTransform.GetComponent<Image>();
            if (cardImg != null)
            {
                // Premium ultra-dark slate-blue glassmorphic card background
                cardImg.color = new Color(0.06f, 0.08f, 0.14f, 0.95f);
            }

            // Soft white/blue outline for a high-end glass edge look
            Outline cardOutline = cajaMenuTransform.GetComponent<Outline>();
            if (cardOutline == null) cardOutline = cajaMenuTransform.gameObject.AddComponent<Outline>();
            cardOutline.effectColor = new Color(1f, 1f, 1f, 0.07f);
            cardOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Elegant deep shadow for elevation
            Shadow cardShadow = cajaMenuTransform.GetComponent<Shadow>();
            if (cardShadow == null) cardShadow = cajaMenuTransform.gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.50f);
            cardShadow.effectDistance = new Vector2(10f, -10f);

            // Decorative label "REGISTRO DE POSTULANTE" in the card
            Transform sectionLabel = cajaMenuTransform.Find("SectionLabel");
            if (sectionLabel == null)
            {
                GameObject labelObj = new GameObject("SectionLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(cajaMenuTransform, false);
                sectionLabel = labelObj.transform;
            }
            
            RectTransform labelRt = sectionLabel.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0.5f, 1f);
            labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, -40f);
            labelRt.sizeDelta = new Vector2(400f, 30f);

            TextMeshProUGUI labelTxt = sectionLabel.GetComponent<TextMeshProUGUI>();
            if (labelTxt != null)
            {
                labelTxt.text = "REGISTRO DE POSTULANTE";
                if (googleSans != null) labelTxt.font = googleSans;
                labelTxt.fontSize = 15f;
                labelTxt.fontStyle = FontStyles.Bold;
                labelTxt.color = new Color(0.58f, 0.64f, 0.74f, 0.85f); // Soft slate description text
                labelTxt.alignment = TextAlignmentOptions.Center;
                labelTxt.characterSpacing = 2.5f;
            }

            // 7. Input Fields Styling (Nombre, Rut - perfectly aligned in the grid)
            StyleInputField(cajaMenuTransform.Find("Nombre"), "Nombre", "Ingresa tu Nombre...", new Vector2(0f, 130f), googleSans);
            StyleInputField(cajaMenuTransform.Find("Rut"), "Rut", "Ingresa tu RUT...", new Vector2(0f, 60f), googleSans);

            // 8. Buttons Relocation and Styling
            Transform btnPasivoTrans = panelMenu.transform.Find("CajaMenu/Entrevistas/Entrevista Pasiva");
            Transform btnAgresivoTrans = panelMenu.transform.Find("CajaMenu/Entrevistas/Entrevista Agresiva");
            
            if (btnPasivoTrans != null) btnPasivoTrans.SetParent(cajaMenuTransform, false);
            else btnPasivoTrans = cajaMenuTransform.Find("Entrevista Pasiva");

            if (btnAgresivoTrans != null) btnAgresivoTrans.SetParent(cajaMenuTransform, false);
            else btnAgresivoTrans = cajaMenuTransform.Find("Entrevista Agresiva");

            // Hide the redundant Entrevistas container
            Transform entrevistasContainer = cajaMenuTransform.Find("Entrevistas");
            if (entrevistasContainer != null) entrevistasContainer.gameObject.SetActive(false);

            // Center and Style Dificultad Buttons in a unified vertical stack!
            if (btnPasivoTrans != null)
            {
                StyleButton(btnPasivoTrans, "ENTREVISTA PASIVA", new Vector2(0f, -20f), new Vector2(400f, 50f), new Color(0.08f, 0.72f, 0.65f, 1f), googleSans); // Vibrant Teal Green
            }
            if (btnAgresivoTrans != null)
            {
                StyleButton(btnAgresivoTrans, "ENTREVISTA AGRESIVA", new Vector2(0f, -85f), new Vector2(400f, 50f), new Color(0.88f, 0.11f, 0.28f, 1f), googleSans); // Premium Crimson Red
            }

            // 9. Floating History Button relocator and outline styling
            Transform btnHistoryTrans = panelMenu.transform.Find("Ver Historial");
            if (btnHistoryTrans != null) btnHistoryTrans.SetParent(cajaMenuTransform, false);
            else btnHistoryTrans = cajaMenuTransform.Find("Ver Historial");

            if (btnHistoryTrans != null)
            {
                // Align perfectly as the third button in the stack with outline styling
                StyleButton(btnHistoryTrans, "VER HISTORIAL DE REPORTES", new Vector2(0f, -155f), new Vector2(400f, 48f), new Color(0.12f, 0.16f, 0.23f, 0.85f), googleSans, true);
            }

            // 10. Error Text Styling
            Transform errorTrans = cajaMenuTransform.Find("TextoError");
            if (errorTrans == null)
            {
                errorTrans = panelMenu.transform.Find("TextoError");
                if (errorTrans != null) errorTrans.SetParent(cajaMenuTransform, false);
            }

            if (errorTrans != null)
            {
                RectTransform errRt = errorTrans.GetComponent<RectTransform>();
                errRt.anchorMin = new Vector2(0.5f, 0.5f);
                errRt.anchorMax = new Vector2(0.5f, 0.5f);
                errRt.pivot = new Vector2(0.5f, 0.5f);
                errRt.anchoredPosition = new Vector2(0f, -220f);
                errRt.sizeDelta = new Vector2(400f, 35f);

                TextMeshProUGUI errTxt = errorTrans.GetComponent<TextMeshProUGUI>();
                if (errTxt != null)
                {
                    errTxt.alignment = TextAlignmentOptions.Center;
                    errTxt.fontSize = 14f;
                    if (googleSans != null) errTxt.font = googleSans;
                    errTxt.color = new Color(0.98f, 0.44f, 0.52f, 1f); // Glowing light rose
                    errTxt.text = "";
                }
            }
        }

        // Notify scene changes so they can be saved
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        
        Debug.Log("[ApplyMenuStyling] Menu UI Beautified successfully!");
    }

    private static void StyleInputField(Transform inputTrans, string debugName, string placeholderVal, Vector2 pos, TMP_FontAsset font)
    {
        if (inputTrans == null)
        {
            Debug.LogWarning($"[ApplyMenuStyling] Input field '{debugName}' not found!");
            return;
        }

        RectTransform inputRt = inputTrans.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.5f, 0.5f);
        inputRt.anchorMax = new Vector2(0.5f, 0.5f);
        inputRt.pivot = new Vector2(0.5f, 0.5f);
        inputRt.anchoredPosition = pos;
        inputRt.sizeDelta = new Vector2(400f, 52f);

        Image inputImg = inputTrans.GetComponent<Image>();
        if (inputImg != null)
        {
            // Slate 800 flat dark input background
            inputImg.color = new Color(0.10f, 0.14f, 0.22f, 1f);
        }

        // Soft outline
        Outline inputOutline = inputTrans.GetComponent<Outline>();
        if (inputOutline == null) inputOutline = inputTrans.gameObject.AddComponent<Outline>();
        inputOutline.effectColor = new Color(1f, 1f, 1f, 0.05f);
        inputOutline.effectDistance = new Vector2(1f, -1f);

        var input = inputTrans.GetComponent<TMP_InputField>();
        if (input != null)
        {
            // Center alignment for actual input text and placeholder!
            if (input.textComponent != null)
            {
                input.textComponent.alignment = TextAlignmentOptions.Center;
                input.textComponent.fontSize = 18f;
                input.textComponent.color = Color.white;
                if (font != null) input.textComponent.font = font;
            }

            if (input.placeholder != null)
            {
                var placeholderTxt = input.placeholder.GetComponent<TextMeshProUGUI>();
                if (placeholderTxt != null)
                {
                    placeholderTxt.text = placeholderVal;
                    placeholderTxt.alignment = TextAlignmentOptions.Center;
                    placeholderTxt.fontSize = 17f;
                    placeholderTxt.color = new Color(0.58f, 0.64f, 0.74f, 0.55f);
                    if (font != null) placeholderTxt.font = font;
                }
            }

            // Adjust Margins of Text Area so characters are beautifully centered vertically
            Transform textAreaTrans = inputTrans.Find("Text Area") ?? inputTrans.Find(debugName);
            if (textAreaTrans != null)
            {
                RectTransform textAreaRt = textAreaTrans.GetComponent<RectTransform>();
                textAreaRt.anchorMin = Vector2.zero;
                textAreaRt.anchorMax = Vector2.one;
                textAreaRt.offsetMin = new Vector2(15f, 5f);
                textAreaRt.offsetMax = new Vector2(-15f, -5f);
            }
        }
    }

    private static void StyleButton(Transform btnTrans, string labelText, Vector2 pos, Vector2 size, Color accentColor, TMP_FontAsset font, bool isOutlineStyle = false)
    {
        RectTransform btnRt = btnTrans.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = pos;
        btnRt.sizeDelta = size;

        Image btnImg = btnTrans.GetComponent<Image>();
        if (btnImg != null)
        {
            btnImg.color = accentColor;
        }

        // Elegant outline / shadow for depth
        Outline btnOutline = btnTrans.GetComponent<Outline>();
        if (btnOutline == null) btnOutline = btnTrans.gameObject.AddComponent<Outline>();
        
        if (isOutlineStyle)
        {
            // Soft light blue/cyan border outline
            btnOutline.effectColor = new Color(0.38f, 0.62f, 0.94f, 0.35f);
            btnOutline.effectDistance = new Vector2(1.2f, -1.2f);
        }
        else
        {
            btnOutline.effectColor = new Color(1f, 1f, 1f, 0.12f);
            btnOutline.effectDistance = new Vector2(1f, -1f);
            
            Shadow btnShadow = btnTrans.GetComponent<Shadow>();
            if (btnShadow == null) btnShadow = btnTrans.gameObject.AddComponent<Shadow>();
            btnShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            btnShadow.effectDistance = new Vector2(3f, -3f);
        }

        TextMeshProUGUI btnTxt = btnTrans.GetComponentInChildren<TextMeshProUGUI>();
        if (btnTxt != null)
        {
            btnTxt.text = labelText;
            btnTxt.alignment = TextAlignmentOptions.Center;
            btnTxt.fontSize = isOutlineStyle ? 14f : 15f;
            
            if (isOutlineStyle)
            {
                btnTxt.color = new Color(0.75f, 0.85f, 0.98f, 1f);
            }
            else
            {
                btnTxt.color = Color.white;
            }
            
            btnTxt.fontStyle = FontStyles.Bold;
            if (font != null) btnTxt.font = font;
            btnTxt.characterSpacing = 1.5f;

            RectTransform txtRt = btnTxt.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
        }

        // Add micro-animation hover component
        var hoverScript = btnTrans.GetComponent<MenuButtonHover>();
        if (hoverScript == null)
        {
            hoverScript = btnTrans.gameObject.AddComponent<MenuButtonHover>();
        }
        
        hoverScript.normalColor = accentColor;
        if (isOutlineStyle)
        {
            hoverScript.hoverColor = new Color(0.18f, 0.24f, 0.35f, 0.95f);
        }
        else
        {
            hoverScript.hoverColor = new Color(
                Mathf.Min(accentColor.r * 1.15f, 1f),
                Mathf.Min(accentColor.g * 1.15f, 1f),
                Mathf.Min(accentColor.b * 1.15f, 1f),
                accentColor.a
            );
        }
    }
}
