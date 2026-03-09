window.flightDetails = {
    notifyResize: function () {
        window.dispatchEvent(new Event("resize"));
    },

    startResize: async function (element, dotNetRef) {
        if (!element || !dotNetRef) return;

        const rect = element.getBoundingClientRect();
        const layoutLeft = rect.left;
        const layoutWidth = rect.width;

        if (!layoutWidth || layoutWidth <= 0) return;

        await dotNetRef.invokeMethodAsync("SetResizeBounds", layoutLeft, layoutWidth);

        const moveHandler = (e) => {
            e.preventDefault();
            dotNetRef.invokeMethodAsync("OnResizeDrag", e.clientX);
        };

        const upHandler = async (e) => {
            e.preventDefault();

            document.removeEventListener("mousemove", moveHandler);
            document.removeEventListener("mouseup", upHandler);

            document.body.style.cursor = "";
            document.body.style.userSelect = "";

            await dotNetRef.invokeMethodAsync("OnResizeEnd");
        };

        document.body.style.cursor = "col-resize";
        document.body.style.userSelect = "none";

        document.addEventListener("mousemove", moveHandler);
        document.addEventListener("mouseup", upHandler);
    }
};