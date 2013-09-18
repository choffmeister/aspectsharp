#!/usr/bin/python

import os
import subprocess
import distutils.core

# configuration
config = 'Release'

# retrieve project base path
script_path = os.path.realpath(__file__)
project_path = os.path.join(os.path.dirname(script_path), '..')
target_path = os.path.join(project_path, 'target')

def remove_folder(path):
	if os.path.exists(path):
		for root, dirs, files in os.walk(path, topdown=False):
			for name in files:
				os.remove(os.path.join(root, name))
			for name in dirs:
				os.rmdir(os.path.join(root, name))
		os.rmdir(path)

def ensure_folder_exists(path):
	if not os.path.exists(path):
		os.makedirs(path)

def build_clean():
	global project_path, target_path

	remove_folder(target_path)
	remove_folder(os.path.join(project_path, 'src', 'AspectSharp.Advices', 'bin'))
	remove_folder(os.path.join(project_path, 'src', 'AspectSharp.Advices', 'obj'))
	remove_folder(os.path.join(project_path, 'src', 'AspectSharp.Weaver', 'bin'))
	remove_folder(os.path.join(project_path, 'src', 'AspectSharp.Weaver', 'obj'))

def build_sources(config):
	global project_path
	src_path = os.path.join(project_path, 'src')

	exit_code = subprocess.call(['xbuild', '/p:Configuration=%s' % config, '/verbosity:quiet', 'AspectSharp.sln'], cwd = src_path)

def package_binaries(config):
	distutils.dir_util.copy_tree(
		os.path.join(project_path, 'src', 'AspectSharp.Weaver', 'bin', config),
		os.path.join(target_path)
	)

build_clean()
ensure_folder_exists(target_path)
build_sources(config)
package_binaries(config)
