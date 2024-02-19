



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
    public static HandleMessage(data: string) {
        let me = this;

        let dataObject = JSON.parse(data);
        if (dataObject.DataType === "SensorData") {
            me.trySetInnerText("cpuData", Math.round(+dataObject.cpuTotal).toString());
        }

        //console.log(`data: ${data}!`)
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

    public static StartYTM() {
        let me = this;

        if (me.ytm == null) {
            me.ytm = window.open("https://music.youtube.com/");
            me.external.sendMessage('FindYTM');
        } else {
            me.external.sendMessage('ToggleYTM');
        }


        //me.external.sendMessage('SendTest');
    }

    public static SendMessage(message: string) {
        let me = this;

        me.external.sendMessage(message);
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
