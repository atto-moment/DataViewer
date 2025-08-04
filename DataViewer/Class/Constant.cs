using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace DataViewer
{
    public struct Constant
    {
        /// <summary>
        /// Joint name list
        /// </summary>
        public static string[] JOINTNAMES =
            [ "pelvis", "abdomen", "thorax",
            "l_clavicle", "l_uarm", "l_larm", "l_hand", "l_lathand", "end:l_hand", "l_medhand", "end:thorax:l_medhand",
            "r_clavicle", "r_uarm", "r_larm", "r_hand", "r_lathand", "end:r_hand", "r_medhand", "end:thorax:r_medhand",
            "neck", "head", "r_ear", "end:head:r_ear", "l_ear", "end:head:l_ear", "l_eye", "end:head:l_eye", "r_eye", "end:head:r_eye", "nose", "end:pelvis:nose",
            "l_thigh", "l_shank", "l_foot", "l_toes", "l_toe", "end:l_toes:l_toe", "l_f_b_toe", "end:l_toes:l_f_b_toe", "l_f_m_toe", "end:pelvis",
            "r_thigh", "r_shank", "r_foot", "r_toes", "r_toe", "end:r_toes:r_toe", "r_f_b_toe", "end:r_toes:r_f_b_toe", "r_f_m_toe", "end:" ];

        /// <summary>
        /// Joint name list (simple version)
        /// </summary>
        public static string[] JOINTNAMES_SIMPLE =
            [ "pelvis", "abdomen", "thorax",
            "l_clavicle", "l_uarm", "r_clavicle", "r_uarm",
            "l_thigh", "l_shank", "l_foot", "r_thigh", "r_shank", "r_foot" ];

        /// <summary>
        /// Body parts count of posture data
        /// </summary>
        public static int BODYPARTS_POSTURE = JOINTNAMES.Length;

        /// <summary>
        /// Header for posture data
        /// </summary>
        public static string[] HEADER_POSTURE = ["timestamp", "jointName", "position_x", "position_y", "position_z"];

        /// <summary>
        /// Dimentions of posture data
        /// </summary>
        public static int DIMENTIONS_POSTURE = HEADER_POSTURE.Length;

        /// <summary>
        /// Header for foot pressure data
        /// </summary>
        public static string[] HEADER_FOOTPRESSURE =
            ["time", "left pressure 1[N/cm2]", "left pressure 2[N/cm2]", "left pressure 3[N/cm2]", "left pressure 4[N/cm2]", "left pressure 5[N/cm2]", "left pressure 6[N/cm2]", "left pressure 7[N/cm2]", "left pressure 8[N/cm2]",
            "left pressure 9[N/cm2]", "left pressure 10[N/cm2]", "left pressure 11[N/cm2]", "left pressure 12[N/cm2]", "left pressure 13[N/cm2]", "left pressure 14[N/cm2]", "left pressure 15[N/cm2]", "left pressure 16[N/cm2]",
            "left acceleration X[g]", "left acceleration Y[g]", "left acceleration Z[g]", "left angular X[dps]", "left angular Y[dps]", "left angular Z[dps]", "left total force[N]", "left center of pressure X[-0.5...+0.5]", "left center of pressure Y[-0.5...+0.5]",
            "right pressure 1[N/cm2]", "right pressure 2[N/cm2]", "right pressure 3[N/cm2]", "right pressure 4[N/cm2]", "right pressure 5[N/cm2]", "right pressure 6[N/cm2]", "right pressure 7[N/cm2]", "right pressure 8[N/cm2]",
            "right pressure 9[N/cm2]", "right pressure 10[N/cm2]", "right pressure 11[N/cm2]", "right pressure 12[N/cm2]", "right pressure 13[N/cm2]", "right pressure 14[N/cm2]", "right pressure 15[N/cm2]", "right pressure 16[N/cm2]",
            "right acceleration X[g]", "right acceleration Y[g]", "right acceleration Z[g]", "right angular X[dps]", "right angular Y[dps]", "right angular Z[dps]", "right total force[N]", "right center of pressure X[-0.5...+0.5]", "right center of pressure Y[-0.5...+0.5]" ];

        /// <summary>
        /// Dimentions of foot pressure data
        /// </summary>
        public static int DIMENTIONS_FOOTPRESSURE = HEADER_FOOTPRESSURE.Length;

        /// <summary>
        /// Nomal viewer
        /// </summary>
        public static Page NOMAL_VIEW = new Viewer();

        /// <summary>
        /// Turn viewer
        /// </summary>
        public static Page TURN_VIEW = new TurnViewer();

        /// <summary>
        /// Analysis viewer
        /// </summary>
        public static Page ANALYSIS_VIEW = new AnalysisViewer();
    }
}
