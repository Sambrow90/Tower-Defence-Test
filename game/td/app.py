from pathlib import Path

from kivy.app import App
from kivy.lang import Builder
from kivy.logger import Logger
from kivy.uix.screenmanager import ScreenManager, SlideTransition
from kivy.core.window import Window
from kivy.core.audio import SoundLoader

from td.tools.assets import ensure_assets, AssetDownloadError, RECOMMENDED_UI_FILES
from td.util.resources import resource_path

class RootScreens(ScreenManager):
    pass

class RandomTDApp(App):
    title = "Modular TD (Kivy)"

    def build(self):
        try:
            fetched = ensure_assets()
            if fetched:
                Logger.info("assets: downloaded %d files", len(fetched))
        except AssetDownloadError as exc:
            Logger.warning("assets: %s", exc)

        self.assets_ready = all(Path(resource_path(*parts)).exists() for parts in RECOMMENDED_UI_FILES)
        font_path = Path(resource_path("assets", "fonts", "MedievalSharp.ttf"))
        self.font_path = str(font_path) if font_path.exists() else ""

        # Window size default
        if Window.width < 1200 or Window.height < 720:
            Window.size = (1280, 720)

        # Load KV files
        Builder.load_file(resource_path("ui", "menu.kv"))
        Builder.load_file(resource_path("ui", "game.kv"))

        from td.screens.menu import MenuScreen   # noqa
        from td.screens.game import GameScreen   # noqa

        root = RootScreens(transition=SlideTransition())
        root.add_widget(MenuScreen(name="menu"))
        root.add_widget(GameScreen(name="game"))
        root.current = "menu"
        self._ui_sound = SoundLoader.load(resource_path("assets", "sfx", "ui_click.wav"))
        return root

    def play_ui_click(self):
        if self._ui_sound:
            self._ui_sound.stop()
            self._ui_sound.play()
