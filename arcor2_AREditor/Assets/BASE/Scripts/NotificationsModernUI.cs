using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Michsky.UI.ModernUIPack;
using System.IO;
using System;

namespace Base {
    public class NotificationsModernUI : Notifications {

        public List<LogEntry> LogEntries = new List<LogEntry>();

        public NotificationManager NotificationManager;

        [SerializeField]
        private Canvas Canvas;

        public GameObject NotificationEntryPrefab, NotificationMenuContent;

        public void Start() {
            //Notification = NotificationManager.gameObject.GetComponent<UIManagerNotification>();
        }
        public override void ShowNotification(string title, string text) {
            // HACK to make notifiaction in foreground
            // TODO - find better way
            Canvas.enabled = false;
            Canvas.enabled = true;
            NotificationManager.title = title;
            NotificationManager.description = text;
            NotificationManager.OpenNotification();
            LogEntries.Add(new LogEntry("Notification", title, text));
            NotificationEntry notificationEntry = Instantiate(NotificationEntryPrefab, NotificationMenuContent.transform).GetComponent<NotificationEntry>();
            notificationEntry.transform.SetAsFirstSibling();
            notificationEntry.Title.text = title;
            notificationEntry.Description.text = text;
            notificationEntry.Timestamp.text = DateTime.Now.ToString();
        }

        private void OnEnable() {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable() {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type) {
            LogEntries.Add(new LogEntry(type.ToString(), logString, stackTrace));
            if (type == LogType.Exception) {
                //automatially create logs in case of exception
                SaveLogs(Scene.Instance.Data, GameManager.Instance.CurrentProject, "Exception occured");
            }
        }

        public override void SaveLogs(IO.Swagger.Model.Scene scene, IO.Swagger.Model.Project project, string customNotificationTitle = "") {
            string sceneString = "", projectString = "";
            if (Scene.Instance.Data != null)
                sceneString = scene.ToJson();
            if (GameManager.Instance.CurrentProject != null)
                projectString = project.ToJson();
            string dirname = Application.persistentDataPath + "/Logs/" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            Directory.CreateDirectory(dirname);
            StreamWriter sceneFile = File.CreateText(dirname + "/scene.json");
            sceneFile.Write(sceneString);
            sceneFile.Close();

            StreamWriter projectFile = File.CreateText(dirname + "/project.json");
            projectFile.Write(projectString);
            projectFile.Close();

            StreamWriter logsFile = File.CreateText(dirname + "/logs.txt");
            foreach (LogEntry log in LogEntries) {
                logsFile.WriteLine("Timestamp: " + log.TimeStamp.ToString());
                logsFile.WriteLine("Type: " + log.LogType.ToString());
                logsFile.WriteLine("Message: " + log.LogMessage);
                if (log.LogType != LogType.Log.ToString())
                    logsFile.WriteLine("Stacktrace: " + log.StackTrace);
                logsFile.WriteLine("");
            }
            logsFile.Close();
            ShowNotification(customNotificationTitle, "Logs saved to directory " + dirname);

        }
    }

}
