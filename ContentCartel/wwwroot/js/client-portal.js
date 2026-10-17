document.addEventListener("DOMContentLoaded", function () {

    const filterGroups = document.querySelectorAll("[data-filter-container]");

    filterGroups.forEach(function (container) {

        const groupName = container.dataset.filterContainer;

        const buttons = document.querySelectorAll(
            `[data-filter-group="${groupName}"]`
        );

        const items = document.querySelectorAll(
            `[data-filter-item="${groupName}"]`
        );

        buttons.forEach(function (button) {

            button.addEventListener("click", function () {

                const selectedFilter =
                    button.dataset.filterValue.toLowerCase();

                // Change active button
                buttons.forEach(function (btn) {
                    btn.classList.remove("active");
                    btn.setAttribute("aria-pressed", "false");
                });

                button.classList.add("active");
                button.setAttribute("aria-pressed", "true");

                // Filter available records
                items.forEach(function (item) {

                    const itemValue =
                        (item.dataset.filterValue || "").toLowerCase();

                    const showItem =
                        selectedFilter === "all" ||
                        itemValue === selectedFilter;

                    item.classList.toggle("d-none", !showItem);
                });

            });

        });

    });

});