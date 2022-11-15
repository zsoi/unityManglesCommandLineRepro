using UnityEngine;
using UnityEngine.UI;

public class CommandLineOutput : MonoBehaviour
{
    public Text DebugText;

    private void OnEnable()
    {
        DebugText.text = System.Environment.CommandLine;

#if WRITE_CMDLINE_AND_QUIT
        {
            System.IO.File.WriteAllText("commandline.txt", System.Environment.CommandLine);
            Application.Quit();
        }
#endif
    }
}