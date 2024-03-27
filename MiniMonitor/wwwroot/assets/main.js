"use strict";
class Util {
    static findATagByInnerText(text) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                return allATags[i];
            }
        }
        return null; // Not found
    }
    static clickATagByInnerText(text) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                allATags[i].click();
            }
        }
    }
    static GetCurrentSong() {
        let node = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string");
        if (node != null) {
            return node.innerText;
        }
        return "";
    }
}
class MyClass {
    static ytm = null;
    static external = window["external"];
    static lastSoundTime = new Date("2000/01/01");
    static trySetInnerText(id, text) {
        let x = document.getElementById(id);
        if (x) {
            x.innerText = text;
        }
    }
    //HandleMessage
    static lastCalendarData = null;
    static lastCalendarInterval = 0;
    static blinkIntervalRef = 0;
    static lastBlinkInterval = 0;
    static BlinkText(elementId, blinkInterval) {
        let me = this;
        const element = document.getElementById(elementId);
        let visible = true;
        if (me.blinkIntervalRef) {
            clearInterval(me.blinkIntervalRef);
        }
        me.blinkIntervalRef = setInterval(() => {
            visible = !visible;
            if (blinkInterval >= 2000) {
                if (visible) {
                    me.BlinkTextTimes(elementId, !visible, 4);
                }
                else {
                    me.BlinkTextTimes(elementId, visible, 4);
                }
            }
            else {
                // just toggle
                element.style.opacity = visible ? "1" : "0";
            }
        }, blinkInterval);
    }
    static BlinkTextTimes(elementId, visible, count) {
        let me = this;
        const element = document.getElementById(elementId);
        visible = !visible;
        element.style.opacity = visible ? "1" : "0";
        if (count > 0) {
            setTimeout(function () {
                me.BlinkTextTimes(elementId, visible, count - 1);
            }, 200);
        }
    }
    static HandleMessage(data) {
        let me = this;
        let dataObject = JSON.parse(data);
        if (dataObject.DataType === "MusicUpdate") {
            this.trySetInnerText("playingSong", dataObject.Song);
        }
        if (dataObject.DataType === "CalendarData") {
            me.lastCalendarData = dataObject;
            if (me.lastCalendarInterval) {
                clearInterval(me.lastCalendarInterval);
            }
            var updateCalData = function () {
                if (dataObject.HasEvents === false) {
                    MyClass.setStyleDisplay("nextMeeting", "none");
                    return;
                }
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
                let soundInterval = 0;
                let blinkInterval = 0;
                if (minutesUntil < 1) {
                    blinkInterval = 500;
                    soundInterval = 30;
                }
                else if (minutesUntil < 2) {
                    blinkInterval = 500;
                    soundInterval = 30;
                }
                else if (minutesUntil < 5) {
                    blinkInterval = 1000;
                    soundInterval = 60;
                }
                else if (minutesUntil < 15) {
                    blinkInterval = 2000;
                    soundInterval = 60;
                }
                else if (minutesUntil < 30) {
                    blinkInterval = 3000;
                }
                else if (minutesUntil < 60) {
                    blinkInterval = 5000;
                }
                if (blinkInterval === 0) {
                    clearInterval(me.blinkIntervalRef);
                }
                else {
                    if (me.lastBlinkInterval !== blinkInterval) {
                        me.lastBlinkInterval = blinkInterval;
                        me.BlinkText('nextMeeting', blinkInterval);
                    }
                }
                debugger;
                if (soundInterval > 0 && minutesUntil > 0) {
                    // only if there is an interval AND the meeting is in the future.
                    let timeDiffSeconds = (new Date().getTime() - me.lastSoundTime.getTime()) / 1000;
                    if (timeDiffSeconds > soundInterval) {
                        me.lastSoundTime = new Date();
                        const audioElement = new Audio('assets/mixkit-short-rooster-crowing-2470.wav');
                        audioElement.play();
                    }
                }
                MyClass.setStyleDisplay("nextMeeting", "block");
            };
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
    static ToggleYTM() {
        let me = this;
        //if (me.ytm == null) {
        //    //me.ytm = window.open("https://music.youtube.com/");
        //    me.external.sendMessage('FindYTM');
        //} else {
        //    me.external.sendMessage('ToggleYTM');
        //}
        me.external.sendMessage('ToggleYTM');
        //me.external.sendMessage('TestWebDriver');
        //me.external.sendMessage('SendTest');
    }
    static SendMessage(message) {
        let me = this;
        me.external.sendMessage(message);
    }
    static UpdateCalendar() {
        let me = this;
        MyClass.setStyleDisplay("nextMeeting", "none");
        if (me.lastCalendarInterval) {
            clearInterval(me.lastCalendarInterval);
        }
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
window["Util"] = Util;
