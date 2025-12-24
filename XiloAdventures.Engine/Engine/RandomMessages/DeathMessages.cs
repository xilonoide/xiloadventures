using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje de muerte por hambre.
    /// </summary>
    public static string HungerDeath => Pick(
        "Has muerto de hambre. Tu cuerpo no pudo resistir más sin alimento.",
        "El hambre ha acabado contigo. Tu cuerpo se rinde ante la inanición.",
        "Tu estómago vacío te ha llevado a la tumba.",
        "La falta de alimento ha sido tu perdición.",
        "Has sucumbido al hambre. Tu cuerpo se apaga lentamente.",
        "El hambre te consume por completo. Todo se vuelve negro.",
        "Sin comida, tu cuerpo ha dejado de funcionar.",
        "La inanición ha reclamado tu vida.",
        "Tu cuerpo, debilitado por el hambre, finalmente se rinde.",
        "Has muerto de inanición. Debiste haber comido algo.",
        "El hambre te ha vencido. Tu viaje termina aquí.",
        "Tu estómago vacío ha sido tu sentencia de muerte."
    );

    /// <summary>
    /// Mensaje de muerte por sed.
    /// </summary>
    public static string ThirstDeath => Pick(
        "Has muerto de sed. La deshidratación ha acabado contigo.",
        "La sed te ha matado. Tu cuerpo seco se desploma.",
        "Sin agua, tu cuerpo ha dejado de funcionar.",
        "La deshidratación ha reclamado tu vida.",
        "Tu garganta reseca ha sido tu perdición.",
        "Has sucumbido a la sed. Tu cuerpo se marchita.",
        "La falta de agua ha sido fatal.",
        "Tu cuerpo, sediento y débil, finalmente se rinde.",
        "Has muerto deshidratado. Debiste haber bebido algo.",
        "La sed te ha vencido. Tu viaje termina aquí.",
        "Sin líquidos, tu cuerpo colapsa.",
        "La sed ha consumido tus últimas fuerzas."
    );

    /// <summary>
    /// Mensaje de muerte por agotamiento (sueño).
    /// </summary>
    public static string SleepDeath => Pick(
        "Has muerto de agotamiento. Tu cuerpo colapsó por falta de sueño.",
        "El cansancio te ha vencido. Tu cuerpo se apaga.",
        "Sin descanso, tu cuerpo ha dejado de funcionar.",
        "El agotamiento ha reclamado tu vida.",
        "Tu cuerpo exhausto finalmente se rinde.",
        "Has sucumbido al cansancio extremo.",
        "La falta de sueño ha sido fatal.",
        "Tu mente agotada se apaga para siempre.",
        "Has muerto de agotamiento. Debiste haber descansado.",
        "El cansancio te ha consumido por completo.",
        "Sin sueño, tu cuerpo colapsa definitivamente.",
        "El agotamiento ha acabado con tus últimas fuerzas."
    );

    /// <summary>
    /// Mensaje de muerte por perder toda la salud.
    /// </summary>
    public static string HealthDeath => Pick(
        "Has muerto. Tus heridas fueron demasiado graves.",
        "Tu cuerpo no ha podido resistir más. Has fallecido.",
        "Las heridas te han llevado a la tumba.",
        "Tu vida se escapa con la última gota de sangre.",
        "Has sucumbido a tus heridas. Todo se vuelve oscuro.",
        "El daño ha sido demasiado. Tu cuerpo se rinde.",
        "Tus heridas mortales han sellado tu destino.",
        "La muerte te reclama. Tu aventura termina aquí.",
        "Has caído. Tu cuerpo maltrecho ya no responde.",
        "Las heridas te han vencido. Exhalas tu último aliento.",
        "Tu cuerpo, destrozado, finalmente cede.",
        "Has muerto por tus heridas. El dolor desaparece al fin."
    );

    /// <summary>
    /// Mensaje de muerte por perder toda la cordura.
    /// </summary>
    public static string SanityDeath => Pick(
        "Tu mente se ha quebrado. La locura te consume por completo.",
        "Has perdido la razón. Tu mente se fragmenta en mil pedazos.",
        "La locura te ha reclamado. Ya no queda nada de ti.",
        "Tu cordura se desvanece. Solo queda el vacío.",
        "Has sucumbido a la demencia. Tu mente colapsa.",
        "La oscuridad mental te engulle por completo.",
        "Tu psique se rompe irreparablemente.",
        "La locura ha ganado. Tu mente ya no te pertenece.",
        "Has enloquecido completamente. No hay vuelta atrás.",
        "Tu mente se pierde en el abismo de la locura.",
        "La cordura te abandona. Solo queda la nada.",
        "Tu razón se extingue. La demencia te consume."
    );
}
