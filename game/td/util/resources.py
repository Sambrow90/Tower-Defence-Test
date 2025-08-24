import os

def resource_path(*parts):
    # Returns an absolute path for resources, robust for 'python main.py' from project root.
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    return os.path.join(base_dir, *parts)
