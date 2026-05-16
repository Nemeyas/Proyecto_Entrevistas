using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public static class AntigravityUIStyling
{
    [MenuItem("Tools/Apply Styling")]
    public static void ApplyStyles()
    {
        GameObject panelReporte = GameObject.Find("PanelReporte");
        if (panelReporte == null) { Debug.LogError("PanelReporte not found"); return; }

        var gestor = panelReporte.GetComponent<GestorReporte>();

        Transform salirBtn = panelReporte.transform.Find("Salir");
        Transform scrollView = panelReporte.transform.Find("Scroll View");
        Transform content = scrollView != null ? (scrollView.Find("Viewport/Content") ?? scrollView.Find("Viewport/Panel")) : null;

        // PanelReporte background
        Image bgImage = panelReporte.GetComponent<Image>();
        if (bgImage != null) bgImage.color = new Color32(238, 242, 255, 255);

        // Salir Button
        if (salirBtn != null) {
            RectTransform rt = salirBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(40, -40);
            rt.sizeDelta = new Vector2(250, 45);
            
            Image btnImg = salirBtn.GetComponent<Image>();
            if (btnImg != null) {
                btnImg.color = Color.white;
                if (btnImg.gameObject.GetComponent<Shadow>() == null) {
                    Shadow sh = btnImg.gameObject.AddComponent<Shadow>();
                    sh.effectColor = new Color(0,0,0,0.1f);
                    sh.effectDistance = new Vector2(0, -2);
                }
            }
            
            TextMeshProUGUI txt = salirBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) {
                txt.text = "<- Volver al Historial";
                txt.color = new Color32(71, 85, 105, 255);
                txt.fontSize = 16;
                txt.alignment = TextAlignmentOptions.Center;
            }
        }

        // Content Layout
        if(content != null) {
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) {
                vlg.padding = new RectOffset(60, 60, 100, 60);
                vlg.spacing = 20;
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childControlWidth = true;
            }
            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        System.Action<Transform, Color32> StyleCard = (t, color) => {
            Image img = t.GetComponent<Image>();
            if (img == null) img = t.gameObject.AddComponent<Image>();
            img.color = color;
            
            if (t.GetComponent<Shadow>() == null) {
                Shadow sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0,0,0,0.05f);
                sh.effectDistance = new Vector2(0, -4);
            }
            
            VerticalLayoutGroup lg = t.GetComponent<VerticalLayoutGroup>();
            if (lg == null) lg = t.gameObject.AddComponent<VerticalLayoutGroup>();
            lg.padding = new RectOffset(40, 40, 40, 40);
            lg.spacing = 15;
            lg.childForceExpandHeight = false;
            lg.childControlHeight = true;
            lg.childForceExpandWidth = true;
            lg.childControlWidth = true;
            
            var csf = t.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = t.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        };

        Transform header = content?.Find("Header");
        Transform resumen = content?.Find("Resumen");
        Transform emocional = content?.Find("Emocional");
        Transform fila = content?.Find("FilaDividida");
        Transform recomendaciones = fila?.Find("Recomendaciones");
        Transform momentos = fila?.Find("MomentosCriticos");

        if (header != null) {
            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) GameObject.DestroyImmediate(hlg);
            
            StyleCard(header, Color.white);
            
            Transform headerTop = header.Find("HeaderTopRow");
            if (headerTop == null) {
                GameObject row = new GameObject("HeaderTopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(header, false);
                row.transform.SetAsFirstSibling();
                var rhlg = row.GetComponent<HorizontalLayoutGroup>();
                rhlg.childControlWidth = true;
                rhlg.childForceExpandWidth = true;
                rhlg.childControlHeight = true;
                rhlg.childForceExpandHeight = false;
                
                Transform titulo = header.Find("Titulo");
                if (titulo != null) {
                    titulo.SetParent(row.transform, false);
                    var txt = titulo.GetComponent<TextMeshProUGUI>();
                    txt.text = "Reporte de Entrevista #1";
                    txt.fontSize = 32;
                    txt.color = new Color32(30, 41, 59, 255);
                    txt.fontStyle = FontStyles.Bold;
                    
                    var le = titulo.gameObject.GetComponent<LayoutElement>() ?? titulo.gameObject.AddComponent<LayoutElement>();
                    le.flexibleWidth = 1;
                }
                
                GameObject scoreBox = new GameObject("ScoreBox", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                scoreBox.transform.SetParent(row.transform, false);
                var sLg = scoreBox.GetComponent<HorizontalLayoutGroup>();
                sLg.childAlignment = TextAnchor.MiddleRight;
                sLg.spacing = 15;
                sLg.childControlWidth = true;
                sLg.childForceExpandWidth = false;
                sLg.childControlHeight = true;
                sLg.childForceExpandHeight = false;
                
                if (gestor.textoPuntaje != null) {
                    gestor.textoPuntaje.transform.SetParent(scoreBox.transform, false);
                    gestor.textoPuntaje.text = "78<size=24><color=#94A3B8>/100</color></size>";
                    gestor.textoPuntaje.color = new Color32(88, 28, 135, 255);
                    gestor.textoPuntaje.fontSize = 48;
                    gestor.textoPuntaje.fontStyle = FontStyles.Bold;
                    gestor.textoPuntaje.alignment = TextAlignmentOptions.MidlineRight;
                }
                if (gestor.textoNotaLetra != null) {
                    GameObject gradeBox = new GameObject("GradeBox", typeof(RectTransform), typeof(Image));
                    gradeBox.transform.SetParent(scoreBox.transform, false);
                    Image bg = gradeBox.GetComponent<Image>();
                    bg.color = new Color32(34, 197, 94, 255);
                    
                    var letRt = gradeBox.GetComponent<RectTransform>();
                    letRt.sizeDelta = new Vector2(80, 80);
                    var le = gradeBox.AddComponent<LayoutElement>();
                    le.minWidth = 80; le.minHeight = 80; le.preferredWidth = 80; le.preferredHeight = 80;

                    gestor.textoNotaLetra.transform.SetParent(gradeBox.transform, false);
                    gestor.textoNotaLetra.color = Color.white;
                    gestor.textoNotaLetra.fontSize = 36;
                    gestor.textoNotaLetra.fontStyle = FontStyles.Bold;
                    gestor.textoNotaLetra.alignment = TextAlignmentOptions.Center;
                    var txtRt = gestor.textoNotaLetra.GetComponent<RectTransform>();
                    txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
                    txtRt.sizeDelta = Vector2.zero;
                    txtRt.anchoredPosition = Vector2.zero;
                }
            }
        }

        if (resumen != null) {
            StyleCard(resumen, Color.white);
            Transform title = resumen.Find("Titulo") ?? new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            title.SetParent(resumen, false);
            title.SetAsFirstSibling();
            var txt = title.GetComponent<TextMeshProUGUI>();
            txt.text = "~ Resumen de la Entrevista";
            txt.color = new Color32(49, 46, 129, 255);
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            
            if (gestor.textoResumen != null) {
                gestor.textoResumen.color = new Color32(71, 85, 105, 255);
                gestor.textoResumen.fontSize = 16;
                gestor.textoResumen.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        if (emocional != null) {
            StyleCard(emocional, Color.white);
            Transform title = emocional.Find("Titulo") ?? new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            title.SetParent(emocional, false);
            title.SetAsFirstSibling();
            var txt = title.GetComponent<TextMeshProUGUI>();
            txt.text = "~ Estado Emocional";
            txt.color = new Color32(49, 46, 129, 255);
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            
            if (gestor.textoEstadoEmocional != null) {
                gestor.textoEstadoEmocional.color = new Color32(71, 85, 105, 255);
                gestor.textoEstadoEmocional.fontSize = 16;
                gestor.textoEstadoEmocional.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        if (fila != null) {
            var hlg = fila.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) {
                hlg.spacing = 20;
                hlg.childControlWidth = true;
                hlg.childForceExpandWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandHeight = true;
                hlg.childAlignment = TextAnchor.UpperCenter;
            }
            var csf = fila.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = fila.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (recomendaciones != null) {
            StyleCard(recomendaciones, new Color32(254, 252, 232, 255));
            Transform title = recomendaciones.Find("Titulo") ?? new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            title.SetParent(recomendaciones, false);
            title.SetAsFirstSibling();
            var txt = title.GetComponent<TextMeshProUGUI>();
            txt.text = "? Recomendaciones";
            txt.color = new Color32(202, 138, 4, 255);
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            
            if (gestor.textoRecomendaciones != null) {
                gestor.textoRecomendaciones.color = new Color32(71, 85, 105, 255);
                gestor.textoRecomendaciones.fontSize = 15;
                gestor.textoRecomendaciones.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        if (momentos != null) {
            StyleCard(momentos, new Color32(254, 242, 242, 255));
            Transform title = momentos.Find("Titulo") ?? new GameObject("Titulo", typeof(RectTransform), typeof(TextMeshProUGUI)).transform;
            title.SetParent(momentos, false);
            title.SetAsFirstSibling();
            var txt = title.GetComponent<TextMeshProUGUI>();
            txt.text = "! Momentos Críticos";
            txt.color = new Color32(220, 38, 38, 255);
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            
            if (gestor.textoMomentosCriticos != null) {
                gestor.textoMomentosCriticos.gameObject.SetActive(false);
            }
            
            for(int i=momentos.childCount-1; i>=0; i--) {
                Transform c = momentos.GetChild(i);
                if (c.name.StartsWith("Critico_")) {
                    if (Application.isPlaying) GameObject.Destroy(c.gameObject);
                    else GameObject.DestroyImmediate(c.gameObject);
                }
            }

            string[] qs = {"¿Dónde trabajaba anteriormente?", "¿Cuál fue tu mayor logro profesional?", "¿Por qué dejaste tu último trabajo?"};
            string[] ws = {"No supo responder con claridad y mostró señales de nerviosismo", "Respuesta vaga sin ejemplos concretos", "Tardó mucho en responder y mostró incomodidad"};
            
            for (int i=0; i<3; i++) {
                GameObject card = new GameObject("Critico_" + i, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                card.transform.SetParent(momentos, false);
                card.GetComponent<Image>().color = Color.white;
                
                var clg = card.GetComponent<VerticalLayoutGroup>();
                clg.padding = new RectOffset(20,20,20,20);
                clg.spacing = 8;
                clg.childControlWidth = true;
                clg.childForceExpandWidth = true;
                clg.childControlHeight = true;
                clg.childForceExpandHeight = false;
                
                var ccsf = card.GetComponent<ContentSizeFitter>();
                ccsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                GameObject q = new GameObject("Q", typeof(RectTransform), typeof(TextMeshProUGUI));
                q.transform.SetParent(card.transform, false);
                var qtxt = q.GetComponent<TextMeshProUGUI>();
                qtxt.text = qs[i];
                qtxt.color = new Color32(30, 41, 59, 255);
                qtxt.fontStyle = FontStyles.Bold;
                qtxt.fontSize = 15;
                qtxt.textWrappingMode = TextWrappingModes.Normal;
                
                GameObject w = new GameObject("W", typeof(RectTransform), typeof(TextMeshProUGUI));
                w.transform.SetParent(card.transform, false);
                var wtxt = w.GetComponent<TextMeshProUGUI>();
                wtxt.text = "<color=#DC2626>!</color> " + ws[i];
                wtxt.color = new Color32(100, 116, 139, 255);
                wtxt.fontSize = 13;
                wtxt.textWrappingMode = TextWrappingModes.Normal;
            }
        }
        
        if (!Application.isPlaying) {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        Debug.Log("Styling applied!");
    }
}
