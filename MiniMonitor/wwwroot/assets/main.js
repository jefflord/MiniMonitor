"use strict";
class Util {
    static MutationObservers = [];
    static findATagByInnerText(text) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                return allATags[i];
            }
        }
        return null; // Not found
    }
    static toggleAc() {
        const url = 'http://10.0.0.79:8222/json'; // Replace <ESP32-IP> with your ESP32's IP address
        // JSON data to be sent in the request
        const data = {
            action: "hitSwitch",
        };
        console.log("start toggleAc");
        fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        })
            .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
            .then(jsonResponse => {
            console.log("response toggleAc");
            console.log('Response:', jsonResponse);
        })
            .catch(error => {
            console.error('Error:', error);
        });
    }
    static clickATagByInnerText(text) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                allATags[i].click();
            }
        }
    }
    static async SendData(data) {
        let image = document.querySelector("mini-monitor-img");
        if (image) {
            image.parentElement?.removeChild(image);
        }
        console.debug("SendData:", JSON.stringify(data, null, 5));
        const url = 'http://127.0.0.1:9191/mini-monitor';
        image = document.createElement("img");
        image.style.display = "none";
        image.src = url + `?data=${encodeURIComponent(JSON.stringify(data))}`;
        image.classList.add("mini-monitor-img");
        document.body.appendChild(image);
    }
    static GetMailboxInfo() {
        let unread = 0;
        let read = 0;
        let latestUnread = null;
        let parseTimeString = function (timeString) {
            // Split the time string into hours, minutes, and AM/PM components
            try {
                if (timeString.indexOf("AM") >= 0 || timeString.indexOf("PM") >= 0 && (timeString.length === 7 || timeString.length === 8)) {
                    timeString = timeString.replace(" ", ":");
                    const [hours, minutes, meridian] = timeString.split(':').map(part => part.trim());
                    // Convert hours to 24-hour format based on AM/PM
                    const adjustedHours = meridian === 'PM' ? (parseInt(hours, 10) + 12) % 24 : +hours;
                    // Create a Date object with provided time and set year, month, and day to 1
                    let date = new Date();
                    date.setHours(adjustedHours);
                    date.setMinutes(+minutes);
                    return date;
                }
            }
            catch (ex) {
            }
            return null;
        };
        //
        document.querySelectorAll(`div:not([attributeName="data-convid"]`).forEach(x => {
            let label = x.getAttribute("aria-label");
            if (label && label.startsWith("Unread")) {
                unread++;
                if (latestUnread === null) {
                    latestUnread = {
                        sender: x.querySelector(".mn28d.XG5Jd > div.JBWmn.gy2aJ.Ejrkd > span")?.innerHTML.trim(),
                        subject: x.querySelector(".ovvvr > div> span")?.innerHTML.trim(),
                        receivedOn: parseTimeString(x.querySelector(".WP8_u > div.lulAg > span")?.innerHTML.trim()),
                    };
                }
            }
            else {
                read++;
            }
        });
        return JSON.stringify({ unread: unread, read: read, latestUnread: latestUnread }, null, 5);
    }
    static GetMutationObserver(fn) {
        let mo = new MutationObserver(fn);
        Util.MutationObservers.push(mo);
        return mo;
    }
    static WatchCurrentSong() {
        let songTag = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string");
        if (songTag != null) {
            let fn = function (ele) {
                let result = { dataType: "GetCurrentSong", found: false };
                let artist = ele.parentElement?.querySelector("span > span.subtitle.style-scope.ytmusic-player-bar > yt-formatted-string > a:nth-child(1)");
                if (ele.innerText) {
                    result.found = true;
                    result.title = ele.innerText;
                    result.artist = artist.innerText;
                }
                let data = {
                    DataType: "MusicUpdate",
                    Success: true,
                    Data: result
                };
                Util.SendData(data);
            };
            if (!songTag.getAttribute("mini-mon-watched")) {
                let options = {
                    childList: false,
                    subtree: false,
                    attributes: true,
                    characterData: true
                };
                let observer = Util.GetMutationObserver(function (mutations) {
                    for (let mutation of mutations) {
                        let changedElement = mutation.target;
                        fn(changedElement);
                    }
                });
                observer.observe(songTag, options);
                songTag.setAttribute("mini-mon-watched", "true");
            }
        }
    }
    static GetCurrentSong() {
        let result = { dataType: "GetCurrentSong", found: false };
        let song = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string");
        let artist = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > span > span.subtitle.style-scope.ytmusic-player-bar > yt-formatted-string > a:nth-child(1)");
        if (song != null && artist != null) {
            result.found = true;
            result.title = song.innerText;
            result.artist = artist.innerText;
        }
        return JSON.stringify(result);
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
            if (dataObject.Success && dataObject.Data.found === true) {
                this.trySetInnerText("playingSong", dataObject.Data.title);
                this.trySetInnerText("playingArtist", dataObject.Data.artist);
            }
            else {
                this.trySetInnerText("playingSong", "Nothing playing...");
                this.trySetInnerText("playingArtist", "");
            }
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
                let soundIntervalMinutes = 0;
                let blinkIntervalMs = 0;
                if (minutesUntil < 1) {
                    blinkIntervalMs = 500;
                    soundIntervalMinutes = 60;
                }
                else if (minutesUntil < 2) {
                    blinkIntervalMs = 500;
                    soundIntervalMinutes = 60;
                }
                else if (minutesUntil < 5) {
                    blinkIntervalMs = 1000;
                    soundIntervalMinutes = 120;
                }
                else if (minutesUntil < 15) {
                    blinkIntervalMs = 2000;
                    soundIntervalMinutes = 120;
                }
                else if (minutesUntil < 30) {
                    soundIntervalMinutes = 300;
                    blinkIntervalMs = 3000;
                }
                else if (minutesUntil < 60) {
                    blinkIntervalMs = 5000;
                }
                if (blinkIntervalMs === 0) {
                    clearInterval(me.blinkIntervalRef);
                }
                else {
                    if (me.lastBlinkInterval !== blinkIntervalMs) {
                        me.lastBlinkInterval = blinkIntervalMs;
                        me.BlinkText('nextMeeting', blinkIntervalMs);
                    }
                }
                if (soundIntervalMinutes > 0 && minutesUntil > 0) {
                    // only if there is an interval AND the meeting is in the future.
                    let timeDiffSeconds = (new Date().getTime() - me.lastSoundTime.getTime()) / 1000;
                    if (timeDiffSeconds > soundIntervalMinutes) {
                        me.lastSoundTime = new Date();
                        const audioElement = new Audio('assets/Alarm04.wav');
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
        else if (dataObject.DataType === "WeatherData") {
            me.trySetInnerText("tempDisplay", `, ${dataObject.Temperature}°`);
        }
        //console.log(`data: ${data}!`)
    }
    static setStyleDisplay(id, value) {
        let ele = document.getElementById(id);
        if (ele != null) {
            ele.style.display = value;
            if (value === "block") {
                ele.style.opacity = "1";
            }
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
