"use strict";
class MyClass {
    static ytm = null;
    static external = window["external"];
    static trySetInnerText(id, text) {
        let x = document.getElementById(id);
        if (x) {
            x.innerText = text;
        }
    }
    //HandleMessage
    static lastCalendarData = null;
    static lastCalendarInterval = 0;
    static HandleMessage(data) {
        let me = this;
        let dataObject = JSON.parse(data);
        if (dataObject.DataType === "CalendarData") {
            me.lastCalendarData = dataObject;
            var updateCalData = function () {
                if (dataObject.HasEvents === false) {
                    MyClass.setStyleDisplay("nextMeeting", "none");
                    return;
                }
                MyClass.setStyleDisplay("nextMeeting", "block");
                me.trySetInnerText("meetingTitle", dataObject.Summary);
                let timeMessage = "";
                let minutesUntil = (new Date(dataObject.StartTimeUtc).getTime() - new Date().getTime()) / 1000.0 / 60.0;
                let formattedRel = (luxon.DateTime.fromISO(dataObject.StartTimeUtc)).toRelative({ base: luxon.DateTime.now(), style: 'long' });
                if (minutesUntil > 0) {
                    timeMessage = `${formattedRel}`;
                }
                else {
                    timeMessage = `started ${formattedRel}`;
                }
                me.trySetInnerText("timeUntilMeeting", timeMessage);
                //me.trySetInnerText("timeUntilMeeting", (minutesUntil).toString());
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingDueSoon");
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingOverDue");
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingInProgress");
                if (minutesUntil <= 0) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingInProgress");
                }
                else if (minutesUntil <= 2) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingOverDue");
                }
                else if (minutesUntil <= 15) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingDueSoon");
                }
                MyClass.setStyleDisplay("nextMeeting", "block");
                //if (dataObject.WaitOneGotSignal === true) {
                //    MyClass.setStyleDisplay("nextMeeting", "none");
                //    setTimeout(function () {
                //        MyClass.setStyleDisplay("nextMeeting", "block");
                //    }, 1000);
                //}
            };
            if (me.lastCalendarInterval) {
                clearInterval(me.lastCalendarInterval);
            }
            me.lastCalendarInterval = setInterval(updateCalData, 1000);
        }
        else if (dataObject.DataType === "SensorData") {
            me.trySetInnerText("cpuData", Math.round(+dataObject.cpuTotal).toString());
        }
        //console.log(`data: ${data}!`)
    }
    static setStyleDisplay(id, value) {
        let ele = document.getElementById(id);
        if (ele != null) {
            ele.style.display = value;
        }
    }
    static async UpdateSensorData() {
        let me = this;
        try {
            const response = await fetch('http://localhost/mini-monitor-data/');
            const responseData = await response.text();
            let data = JSON.parse(responseData);
            me.trySetInnerText("cpuData", Math.round(+data.cpuTotal).toString());
        }
        catch (ex) {
            console.error(ex);
        }
        setTimeout(function () { me.UpdateSensorData(); }, 1000);
    }
    static Close() {
        let me = this;
        me.external.sendMessage('Close');
    }
    static StartYTM() {
        let me = this;
        if (me.ytm == null) {
            me.ytm = window.open("https://music.youtube.com/");
            me.external.sendMessage('FindYTM');
        }
        else {
            me.external.sendMessage('ToggleYTM');
        }
        //me.external.sendMessage('SendTest');
    }
    static SendMessage(message) {
        let me = this;
        me.external.sendMessage(message);
    }
    static UpdateCalendar() {
        let me = this;
        MyClass.setStyleDisplay("nextMeeting", "none");
        me.external.sendMessage("UpdateCalendar");
    }
    static PlayPause() {
        let me = this;
        me.external.sendMessage('PlayPause');
    }
    static doX() {
        alert("doX1");
        return "X";
    }
}
