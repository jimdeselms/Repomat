require 'albacore'
require 'rake/clean'

DOT_NET_PATH = "#{ENV["SystemRoot"]}\\Microsoft.NET\\Framework\\v4.0.30319"
NUNIT_EXE = "packages/NUnit.Runners.2.6.3/tools/nunit-console.exe"
NUGET_EXE = ".nuget/nuget.exe"
SOURCE_PATH = "../src"
OUTPUT_PATH = "output"
CONFIG = "Debug"
 
# CLEAN.include(OUTPUT_PATH)

task :default => ["clean", "build:all"]
 
namespace :build do
  
  task :all => [:restore, :compile, :test]
      
  desc "Build solutions using MSBuild"
  task :compile do
    solutions = FileList["*.sln"]
    solutions.each do |solution|
      sh "#{DOT_NET_PATH}/msbuild.exe /p:Configuration=#{CONFIG} #{solution}"
    end
  end
   
  desc "Runs tests with NUnit"
  task :test => [:compile] do
    tests = FileList["./**/*.UnitTests.dll"].exclude(/obj\//)
    sh "if not exist #{OUTPUT_PATH} mkdir #{OUTPUT_PATH}"	
    sh "#{NUNIT_EXE} #{tests} /nologo /xml=#{OUTPUT_PATH}/TestResults.xml"
  end

  
  desc "Restore NuGet packages"
  task :restore do
    sh "#{NUGET_EXE} restore"
  end

  desc "Pack NuGet package"
  task :pack do
    sh "PackNuget.bat"
  end

end

msbuild :build do |b|
	b.properties = { :configuration => :Debug }
	b.targets = [ :Build ]
	b.solution = "Spededebe.sln"
end

