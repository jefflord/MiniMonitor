
declare let luxon: any;

class Util {
    public static findATagByInnerText(text: string) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                return allATags[i];
            }
        }
        return null; // Not found
    }

    public static clickATagByInnerText(text: string) {
        const allATags = document.querySelectorAll("a");
        for (let i = 0; i < allATags.length; i++) {
            if (allATags[i].innerText.trim() === text.trim()) {
                allATags[i].click();
            }
        }
    }

    public static GetCurrentSong(): string {
        let node = document.querySelector("#layout > ytmusic-player-bar > div.middle-controls.style-scope.ytmusic-player-bar > div.content-info-wrapper.style-scope.ytmusic-player-bar > yt-formatted-string") as HTMLDivElement;
        if (node != null) {
            return node.innerText;
        }
        return "";
    }
}

class MyClass {

    static ytm: Window | null = null;
    static external = window["external"] as any;

    static trySetInnerText(id: string, text: string) {

        let x = document.getElementById(id) as HTMLHtmlElement;
        if (x) {
            x.innerText = text;
        }
    }

    //HandleMessage

    static lastCalendarData = null;
    static lastCalendarInterval = 0;

    public static HandleMessage(data: string) {
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
                let minutesUntil = (new Date(dataObject.StartTimeUtc as string).getTime() - new Date().getTime()) / 1000.0 / 60.0;

                let formattedRel = (luxon.DateTime.fromISO(dataObject.StartTimeUtc)).toRelative({ base: luxon.DateTime.now(), style: 'long' });
                if (minutesUntil > 0) {
                    timeMessage = `${formattedRel}`;
                } else {
                    timeMessage = `started ${formattedRel}`;
                }


                me.trySetInnerText("timeUntilMeeting", timeMessage);
                //me.trySetInnerText("timeUntilMeeting", (minutesUntil).toString());

                document.getElementById("nextMeeting")?.classList.remove("nextMeetingDueSoon");
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingOverDue");
                document.getElementById("nextMeeting")?.classList.remove("nextMeetingInProgress");

                if (minutesUntil <= 0) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingInProgress");
                } else if (minutesUntil <= 2) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingOverDue");
                } else if (minutesUntil <= 15) {
                    document.getElementById("nextMeeting")?.classList.add("nextMeetingDueSoon");
                }


                MyClass.setStyleDisplay("nextMeeting", "block");
            }


            me.lastCalendarInterval = setInterval(updateCalData, 1000);


        } else if (dataObject.DataType === "SensorData") {
            me.trySetInnerText("cpuData", Math.round(+dataObject.cpuTotal).toString());
        }

        //console.log(`data: ${data}!`)
    }

    private static setStyleDisplay(id: string, value: string) {

        let ele = document.getElementById(id);
        if (ele != null) {
            ele.style.display = value;
        }
    }

    public static async UpdateSensorData() {
        let me = this;

        try {
            const response: Response = await fetch('http://localhost/mini-monitor-data/');
            const responseData: string = await response.text();
            let data = JSON.parse(responseData);
            me.trySetInnerText("cpuData", Math.round(+data.cpuTotal).toString());

        } catch (ex) {
            console.error(ex)
        }

        setTimeout(function () { me.UpdateSensorData() }, 1000);
    }

    public static Close() {
        let me = this;

        me.external.sendMessage('Close');
    }

    public static ToggleYTM() {
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

    public static SendMessage(message: string) {
        let me = this;

        me.external.sendMessage(message);
    }

    public static UpdateCalendar() {
        let me = this;
        MyClass.setStyleDisplay("nextMeeting", "none");
        if (me.lastCalendarInterval) {
            clearInterval(me.lastCalendarInterval);
        }
        me.external.sendMessage("UpdateCalendar");
    }


    public static PlayPause() {
        let me = this;

        me.external.sendMessage('PlayPause');
    }

    public static doX(): string {

        alert("doX1");
        return "X";
    }
}

window["Util"] = Util;