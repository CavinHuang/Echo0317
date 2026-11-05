using System;

namespace Echo0317.Core
{
    /// <summary>
    /// Unified back behavior for keyboard/controller/modal close buttons.
    /// Keeps a pause-menu parent while the player moves between its pages,
    /// and returns safely to a confirmation's originating page.
    /// </summary>
    public sealed class EchoModalFlow
    {
        const string ConfirmationPrefix = "confirm:";
        string current = "";
        string parent = "";

        public string Current => current;
        public bool IsOpen => current.Length != 0;

        public void Open(string page)
        {
            if (string.IsNullOrWhiteSpace(page) || page.StartsWith(ConfirmationPrefix, StringComparison.Ordinal))
                throw new ArgumentException("Invalid modal page.", nameof(page));

            if (page == "pause") parent = "";
            else if (current == "pause") parent = "pause";

            current = page;
        }

        public void Confirm(string cancelTo)
        {
            if (!string.IsNullOrEmpty(cancelTo) && cancelTo.StartsWith(ConfirmationPrefix, StringComparison.Ordinal))
                throw new ArgumentException("Invalid confirmation parent.", nameof(cancelTo));
            current = ConfirmationPrefix + (cancelTo ?? "");
        }

        /// <returns>A page to show; empty means close modal and resume the underlying view.</returns>
        public string Back()
        {
            if (!IsOpen) return "";
            if (current.StartsWith(ConfirmationPrefix, StringComparison.Ordinal))
            {
                var destination = current.Substring(ConfirmationPrefix.Length);
                if (destination.Length == 0) { Reset(); return ""; }
                current = destination;
                return current;
            }
            if (parent == "pause" && current != "pause")
            {
                parent = "";
                current = "pause";
                return current;
            }
            Reset();
            return "";
        }

        public void Reset()
        {
            current = "";
            parent = "";
        }
    }
}
