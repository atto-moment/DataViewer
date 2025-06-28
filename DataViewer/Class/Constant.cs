using System.Windows.Controls;

namespace DataViewer.Class
{
    public struct Constant
    {
        /// <summary>
        /// Dimentions of posture data
        /// </summary>
        public static int DIMENTIONS_POSTURE = 5;

        /// <summary>
        /// Body parts count of posture data
        /// </summary>
        public static int BODYPARTS_POSTURE = 51;

        /// <summary>
        /// Dimentions of foot pressure data
        /// </summary>
        public static int DIMENTIONS_FOOTPRESSURE = 51;

        /// <summary>
        /// Nomal viewer
        /// </summary>
        public static Page NOMAL_VIEW = new Viewer();

        /// <summary>
        /// Turn viewer
        /// </summary>
        public static Page TURN_VIEW = new TurnViewer();
    }
}
