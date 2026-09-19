# SIMU1 - Reglas de Proyecto (Simulador Balístico)

Consulte el documento principal de directrices en [SIMU1_RULES.md](file:///f:/Unity/Simu1/SIMU1_RULES.md).

- **Motor:** Unity 6 (`6000.3.12f1`) + URP 17.3.
- **Entrada:** Nuevo Input System.
- **Arquitectura:** MVC (`Simu1.Model` POCO puro sin MonoBehaviour, `Simu1.View`, `Simu1.Controller`), Object Pooling (`Simu1.Pooling`).
- **Integridad:** Respetar `.meta` y usar `[FormerlySerializedAs]` al renombrar campos `[SerializeField] private`.
