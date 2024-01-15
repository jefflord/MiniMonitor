



class MyClass {

    static ytm: Window | null = null;
    static external = window["external"] as any;

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
            me.external.sendMessage('ShowYTM');
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
