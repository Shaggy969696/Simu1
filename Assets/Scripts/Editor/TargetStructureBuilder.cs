using System.IO;
using Simu1.Controller;
using Simu1.Targets;
using UnityEditor;
using UnityEngine;

namespace Simu1.Editor
{
    /// <summary>
    /// Herramienta de Editor para construir automáticamente la estructura de objetivos físicos en la escena.
    /// Diseñada para que el usuario pueda modificarla manualmente de forma intuitiva:
    /// - Cada pieza tiene nombres claros en español y jerarquía ordenada.
    /// - Integra los 3 tipos de uniones: FixedJoint, HingeJoint y SpringJoint.
    /// - Base fija para garantizar estabilidad inicial absoluta (0 colapsos espontáneos).
    /// - Guardado automático como Prefab en Assets/Prefab/Estructura_Objetivos.prefab.
    /// </summary>
    public static class TargetStructureBuilder
    {
        [InitializeOnLoadMethod]
        private static void AutoEnsureStructure()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying) return;
                if (GameObject.Find("[Estructura_Objetivos]") == null)
                {
                    CreateTargetStructure();
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    }
                }
            };
        }

        [MenuItem("Balistica/Crear Estructura de Objetivos en Escena", false, 12)]
        [MenuItem("GameObject/Balistica/Crear Estructura de Objetivos", false, 12)]
        public static GameObject CreateTargetStructure()
        {
            // 0. Si ya existe una previa, reemplazarla de forma limpia
            GameObject existing = GameObject.Find("[Estructura_Objetivos]");
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            // 1. Cargar materiales disponibles en el proyecto para visuales atractivos
            Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_Blocs.mat");
            Material roughMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/YughuesFreePavementsMaterials/Materials/M_YFPM_Rough01.mat");
            Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/Floor.mat");
            Material targetMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiales/New Material.mat");

            // 2. Crear GameObject raíz en la línea de fuego del cañón
            // El cañón se encuentra en Z ≈ -4.65 disparando hacia +X.
            // Posicionamos la estructura a 25 metros frente al cañón.
            GameObject rootGO = new GameObject("[Estructura_Objetivos]");
            rootGO.transform.position = new Vector3(25f, 0f, -4.65f);
            rootGO.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(rootGO, "Crear Estructura de Objetivos");

            TargetStructureManager structureManager = rootGO.AddComponent<TargetStructureManager>();

            // 3. Plataforma Base (Anclaje cinemático para estabilidad inicial absoluta)
            GameObject baseGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseGO.name = "Plataforma_Base";
            baseGO.transform.SetParent(rootGO.transform, false);
            baseGO.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            baseGO.transform.localScale = new Vector3(6.0f, 0.4f, 3.0f);
            if (floorMat != null) baseGO.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            baseGO.tag = "Ground";

            Rigidbody baseRb = baseGO.AddComponent<Rigidbody>();
            baseRb.isKinematic = true;
            baseRb.useGravity = false;

            // Carpeta contenedora de columnas para jerarquía limpia
            GameObject colFolder = new GameObject("Columnas");
            colFolder.transform.SetParent(rootGO.transform, false);

            // 4. Columna Izquierda con FixedJoints
            GameObject colIzqBase = CreateBlock(
                parent: colFolder.transform,
                name: "Columna_Izq_Base",
                localPos: new Vector3(-1.8f, 0.9f, 0f),
                scale: new Vector3(0.8f, 1.0f, 0.8f),
                mass: 12f,
                material: stoneMat,
                pointValue: 100
            );
            FixedJoint fjIzqBase = colIzqBase.AddComponent<FixedJoint>();
            fjIzqBase.connectedBody = baseRb;
            fjIzqBase.breakForce = 750f;
            fjIzqBase.breakTorque = 750f;

            GameObject colIzqTop = CreateBlock(
                parent: colFolder.transform,
                name: "Columna_Izq_Superior",
                localPos: new Vector3(-1.8f, 1.9f, 0f),
                scale: new Vector3(0.8f, 1.0f, 0.8f),
                mass: 9f,
                material: stoneMat,
                pointValue: 150
            );
            FixedJoint fjIzqTop = colIzqTop.AddComponent<FixedJoint>();
            fjIzqTop.connectedBody = colIzqBase.GetComponent<Rigidbody>();
            fjIzqTop.breakForce = 500f;
            fjIzqTop.breakTorque = 500f;

            // 5. Columna Derecha con FixedJoints
            GameObject colDerBase = CreateBlock(
                parent: colFolder.transform,
                name: "Columna_Der_Base",
                localPos: new Vector3(1.8f, 0.9f, 0f),
                scale: new Vector3(0.8f, 1.0f, 0.8f),
                mass: 12f,
                material: stoneMat,
                pointValue: 100
            );
            FixedJoint fjDerBase = colDerBase.AddComponent<FixedJoint>();
            fjDerBase.connectedBody = baseRb;
            fjDerBase.breakForce = 750f;
            fjDerBase.breakTorque = 750f;

            GameObject colDerTop = CreateBlock(
                parent: colFolder.transform,
                name: "Columna_Der_Superior",
                localPos: new Vector3(1.8f, 1.9f, 0f),
                scale: new Vector3(0.8f, 1.0f, 0.8f),
                mass: 9f,
                material: stoneMat,
                pointValue: 150
            );
            FixedJoint fjDerTop = colDerTop.AddComponent<FixedJoint>();
            fjDerTop.connectedBody = colDerBase.GetComponent<Rigidbody>();
            fjDerTop.breakForce = 500f;
            fjDerTop.breakTorque = 500f;

            // 6. Viga Superior con FixedJoints
            GameObject viga = CreateBlock(
                parent: colFolder.transform,
                name: "Viga_Superior",
                localPos: new Vector3(0f, 2.65f, 0f),
                scale: new Vector3(4.8f, 0.5f, 1.0f),
                mass: 16f,
                material: roughMat,
                pointValue: 200
            );
            FixedJoint fjViga = viga.AddComponent<FixedJoint>();
            fjViga.connectedBody = colIzqTop.GetComponent<Rigidbody>();
            fjViga.breakForce = 450f;
            fjViga.breakTorque = 450f;

            // Carpeta contenedora de dianas / mecanismos articulados
            GameObject mechFolder = new GameObject("Mecanismos_Articulados");
            mechFolder.transform.SetParent(rootGO.transform, false);

            // 7. Diana Giratoria con HingeJoint (Péndulo articulado oscilante)
            GameObject dianaHinge = CreateBlock(
                parent: mechFolder.transform,
                name: "Diana_Giratoria_Hinge",
                localPos: new Vector3(-0.85f, 1.6f, 0f),
                scale: new Vector3(0.85f, 0.85f, 0.15f),
                mass: 5f,
                material: targetMat,
                pointValue: 250
            );
            HingeJoint hj = dianaHinge.AddComponent<HingeJoint>();
            hj.connectedBody = viga.GetComponent<Rigidbody>();
            hj.anchor = new Vector3(0f, 0.55f, 0f);
            hj.axis = new Vector3(0f, 0f, 1f); // Eje Z para oscilar libremente al ser impactada en X
            hj.useSpring = true;
            JointSpring springHinge = new JointSpring
            {
                spring = 8f,
                damper = 1.0f,
                targetPosition = 0f
            };
            hj.spring = springHinge;
            hj.breakForce = 600f;
            hj.breakTorque = 600f;

            // 8. Diana Elástica con SpringJoint (Diana elástica amortiguada suspendida)
            GameObject dianaSpring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dianaSpring.name = "Diana_Elastica_Spring";
            dianaSpring.transform.SetParent(mechFolder.transform, false);
            dianaSpring.transform.localPosition = new Vector3(0.85f, 1.6f, 0f);
            dianaSpring.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            if (targetMat != null) dianaSpring.GetComponent<MeshRenderer>().sharedMaterial = targetMat;
            dianaSpring.tag = "Target";

            Rigidbody rbSpring = dianaSpring.AddComponent<Rigidbody>();
            rbSpring.mass = 4.5f;
            rbSpring.interpolation = RigidbodyInterpolation.Interpolate;
            rbSpring.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            TargetPiece tpSpring = dianaSpring.AddComponent<TargetPiece>();
            SerializedObject soSpring = new SerializedObject(tpSpring);
            soSpring.FindProperty("pointValue").intValue = 250;
            soSpring.FindProperty("fallDistanceThreshold").floatValue = 1.2f;
            soSpring.ApplyModifiedProperties();

            SpringJoint sj = dianaSpring.AddComponent<SpringJoint>();
            sj.connectedBody = viga.GetComponent<Rigidbody>();
            sj.autoConfigureConnectedAnchor = true;
            sj.anchor = new Vector3(0f, 0.4f, 0f);
            sj.spring = 200f;
            sj.damper = 8.0f;
            sj.minDistance = 0f;
            sj.maxDistance = 1.2f;
            sj.breakForce = 2500f;

            // 9. Auto-poblar el manager de la estructura
            structureManager.AutoPopulatePieces();
            EditorUtility.SetDirty(structureManager);

            // 10. Vincular automáticamente con BallisticController si existe en la escena
            BallisticController ctrl = Object.FindFirstObjectByType<BallisticController>();
            if (ctrl != null)
            {
                SerializedObject soCtrl = new SerializedObject(ctrl);
                SerializedProperty propManager = soCtrl.FindProperty("targetStructureManager");
                if (propManager != null)
                {
                    propManager.objectReferenceValue = structureManager;
                    soCtrl.ApplyModifiedProperties();
                }
            }

            // 11. Guardar como Prefab modular en Assets/Prefab
            string prefabDir = "Assets/Prefab";
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
            }
            string prefabPath = Path.Combine(prefabDir, "Estructura_Objetivos.prefab").Replace("\\", "/");
            PrefabUtility.SaveAsPrefabAsset(rootGO, prefabPath);

            Selection.activeGameObject = rootGO;
            Debug.Log($"[Estructura Objetivos] Estructura creada con éxito y guardada como prefab en '{prefabPath}'. Total piezas registradas: {structureManager.TotalPiecesCount}");

            return rootGO;
        }

        private static GameObject CreateBlock(
            Transform parent, 
            string name, 
            Vector3 localPos, 
            Vector3 scale, 
            float mass, 
            Material material, 
            int pointValue)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPos;
            block.transform.localScale = scale;
            if (material != null) block.GetComponent<MeshRenderer>().sharedMaterial = material;
            block.tag = "Target";

            Rigidbody rb = block.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            TargetPiece piece = block.AddComponent<TargetPiece>();
            SerializedObject so = new SerializedObject(piece);
            so.FindProperty("pointValue").intValue = pointValue;
            so.FindProperty("fallDistanceThreshold").floatValue = 1.0f;
            so.FindProperty("tiltAngleThreshold").floatValue = 35.0f;
            so.ApplyModifiedProperties();

            return block;
        }
    }
}
